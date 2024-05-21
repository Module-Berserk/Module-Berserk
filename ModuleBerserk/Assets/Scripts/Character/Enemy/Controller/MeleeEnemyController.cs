using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

// 근접 공격을 하는 잡몹의 행동 패턴을 정의하는 클래스.
//
// 의존성:
// 1. IMeleeEnemyBehavior 인터페이스를 구현한 스크립트 추가
// 2. 자식 오브젝트에 인식 판정에 사용할 PlayerDetectionRange 스크립트 추가
// 3. 또 다른 자식 오브젝트에 공격 범위 판정에 사용할 PlayerDetectionRange 스크립트 추가
//
// Hierarchy에서 보면 오브젝트 구조가 아래와 같음:
//
//   melee enemy
//   ㄴ player detection range
//   ㄴ attack range
//
// 만약 적절한 Behavior 스크립트가 없는 상태에서 이 스크립트를
// 게임 오브젝트에 추가하는 경우 아래와 같은 경고 창이 표시된다:
//
//   Can't add script behaviour 'IMeleeEnemyBehavior'.
//   The script class can't be abstract!
//
[RequireComponent(typeof(IMeleeEnemyBehavior))]
[RequireComponent(typeof(FlashEffectOnHit))]
public class MeleeEnemyController : MonoBehaviour, IDestructible
{
    [Header("Player Detectors")]
    // 플레이어가 존재한다는걸 인식할 때 사용하는 detector.
    // idle -> chase 상태 전환 조건으로 쓰인다.
    [SerializeField] private PlayerDetectionRange playerDetectionRange;
    // 플레이어가 공격 범위 안에 있는지 판단하기 위한 detector.
    // chase -> attack 상태 전환 조건으로 쓰인다.
    [SerializeField] private PlayerDetectionRange attackRange;


    [Header("Patrol")]
    // 순찰 상태가 지속되는 최대 시간
    [SerializeField] private float maxPatrolDuration = 6f;
    // 순찰 중 걷기 상태가 지속되는 시간. 걷기 <-> 대기는 번갈아가며 실행됨.
    [SerializeField] private float patrolWalkDuration = 1f;
    // 순찰 중 대기 상태가 지속되는 시간. 걷기 <-> 대기는 번갈아가며 실행됨.
    [SerializeField] private float patrolPauseDuration = 2f;
    // 순찰 중 걷기 상태의 이동 속도
    [SerializeField] private float patrolSpeed = 0.5f;

    // 컴포넌트 레퍼런스
    private IMeleeEnemyBehavior meleeEnemyBehavior;
    private FlashEffectOnHit flashEffectOnHit;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    // IDestructible이 요구하는 스탯
    private CharacterStat hp = new(100f, 0f, 100f);
    private CharacterStat defense = new(10f, 0f);

    // 순찰 상태가 유지된 시간 총합
    private float patrolDuration = 0f;
    // 순찰 세부 상태 중 '걷기' 또는 '대기'가 유지된 시간
    private float patrolSubbehaviorDuration = 0f;
    // 순찰 세부 상태가 '걷기'인지 기록하는 플래그
    private bool isPatrolSubbehaviorWalk = true;

    private enum State
    {
        Idle,
        Chase,
        Attack,
        Stagger,
        ReturnToInitialPosition,
        Patrol,
    }
    private State state = State.Idle;

    private void Awake()
    {
        meleeEnemyBehavior = GetComponent<IMeleeEnemyBehavior>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        playerDetectionRange.OnPlayerDetect.AddListener(HandlePlayerDetection);
    }

    private void FixedUpdate()
    {
        UpdateDetectorDirection();

        if (state == State.Chase)
        {
            // 추적 가능한 범위에 있다면 플레이어에게 접근
            if (meleeEnemyBehavior.CanChasePlayer())
            {
                meleeEnemyBehavior.Chase();

                // 플레이어가 공격 범위에 들어오면 즉시 공격 시도
                if (attackRange.IsPlayerInRange)
                {
                    state = State.Attack;
                }
            }
            // 추적 범위를 벗어났다면 초기 위치로 돌아온 뒤 대기 상태로 전환
            else
            {
                state = State.ReturnToInitialPosition;

                // 돌아가는 동안 플레이어를 발견하면 다시 주위에 알려줘야 함
                playerDetectionRange.IsDetectionShared = true;
            }
        }
        else if (state == State.ReturnToInitialPosition)
        {
            bool isReturnComplete = meleeEnemyBehavior.ReturnToInitialPosition();
            if (isReturnComplete)
            {
                state = State.Patrol;
                patrolDuration = 0f;
                isPatrolSubbehaviorWalk = true;
            }
        }
        else if (state == State.Patrol)
        {
            // 아직 순찰이 끝나지 않은 경우 '걷기'와 '대기'라는 순찰 하위 상태를 반복함
            if (patrolDuration < maxPatrolDuration)
            {
                patrolDuration += Time.fixedDeltaTime;
                HandlePatrolBehavior();
            }
            // 순찰이 끝났다면 다시 대기 상태로 전환
            else
            {
                state = State.Idle;
                meleeEnemyBehavior.StartIdle();
            }
        }
        else if (state == State.Attack)
        {
            // 플레이어가 공격 범위 안에 있고 공격 쿨타임이 지났다면 다음 공격 시행
            if (attackRange.IsPlayerInRange)
            {
                if (meleeEnemyBehavior.IsMeleeAttackReady())
                {
                    meleeEnemyBehavior.MeleeAttack();
                }
            }
            // 공격 범위를 벗어났고, 아직 공격 모션이 재생 중이지 않다면 다시 Chase 상태로 전환
            else if (meleeEnemyBehavior.IsMeleeAttackMotionFinished())
            {
                state = State.Chase;
            }
        }
        else if (state == State.Stagger)
        {
            // 경직과 같이 부여된 넉백 효과 부드럽게 감소
            float updatedVelocityX = Mathf.MoveTowards(rb.velocity.x, 0f, 30f * Time.deltaTime);
            rb.velocity = new Vector2(updatedVelocityX, rb.velocity.y);
        }
    }

    private void HandlePatrolBehavior()
    {
        // '걷기' 또는 '대기'가 지속된 시간 누적
        patrolSubbehaviorDuration += Time.fixedDeltaTime;

        // '걷기'가 끝났으면 '대기' 시작
        if (isPatrolSubbehaviorWalk && patrolSubbehaviorDuration > patrolWalkDuration)
        {
            isPatrolSubbehaviorWalk = false;
            patrolSubbehaviorDuration = 0f;
        }
        // '대기'가 끝났으면 '걷기' 시작
        else if (!isPatrolSubbehaviorWalk && patrolSubbehaviorDuration > patrolPauseDuration)
        {
            isPatrolSubbehaviorWalk = true;
            patrolSubbehaviorDuration = 0f;

            // 순찰 방향은 '걷기' 상태에 진입할 때마다 반대로 바뀜
            patrolSpeed *= -1;
        }

        if (isPatrolSubbehaviorWalk)
        {
            meleeEnemyBehavior.Patrol(patrolSpeed);
        }
    }

    // 플레이어 인식 범위와 공격 가능 범위의 방향을
    // 스프라이트가 바라보는 방향과 일치하도록 조정
    private void UpdateDetectorDirection()
    {
        playerDetectionRange.SetDetectorDirection(spriteRenderer.flipX);
        attackRange.SetDetectorDirection(spriteRenderer.flipX);
    }

    void HandlePlayerDetection()
    {
        if (state == State.Idle)
        {
            state = State.Chase;

            // TODO: 로그 출력 삭제하고 인식 모션 시작
            Debug.Log("플레이어 인식!");
        }

        // 주변에서 인식 정보를 공유받아 이 함수가 호출된 경우
        // 자신의 PlayerDetectionRange에는 플레이어가 아직 감지되지 않은 상태이므로
        // 나중에 OnPlayerDetect가 한 번 더 실행될 수 있음.
        //
        // ex) 다른 층에 있는 적에게서 플레이어 인식 정보를 공유받은 뒤
        //     플레이어가 자신의 층으로 점프해 올라온 경우
        //
        // ex) 대기 상태에서 플레이어에게 백어택을 당해 경직 & 추적 상태에 돌입.
        //     Chase()에 의해 뒤를 돌아본 순간 플레이어가 PlayerDetectionRange 안에 들어온 경우.
        //
        // 하지만 인식 정보 공유는 최초 발견자만 수행해야 하므로
        // 여기서 PlayerDetectionRange의 정보 공유 옵션을 비활성화 해줘야 함.
        playerDetectionRange.IsDetectionShared = false;
    }

    CharacterStat IDestructible.GetHPStat()
    {
        return hp;
    }

    CharacterStat IDestructible.GetDefenseStat()
    {
        return defense;
    }

    Team IDestructible.GetTeam()
    {
        return Team.Enemy;
    }

    void IDestructible.OnDamage(float finalDamage, StaggerInfo staggerInfo)
    {
        // 피격 이펙트
        flashEffectOnHit.StartEffectAsync().Forget();

        // 경직 상태에 들어갔다면 잠시 기다렸다가 추적 시작
        if (meleeEnemyBehavior.TryApplyStagger(staggerInfo))
        {
            state = State.Stagger;

            // TODO: 경직 지속 시간을 staggerInfo의 정보로 대체하기
            StartChasingAfterDuration(0.5f).Forget();
        }
        // 만약 슈퍼아머로 인해 경직을 무시했다면 바로 추적 시작
        else
        {
            state = State.Chase;
        }
    }

    private async UniTask StartChasingAfterDuration(float duration)
    {
        await UniTask.WaitForSeconds(duration);

        state = State.Chase;
    }

    void IDestructible.OnDestruction()
    {
        Destroy(gameObject);
    }
}
