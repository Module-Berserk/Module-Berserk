using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

// 근접 공격을 하는 잡몹의 행동 패턴을 정의하는 클래스.
//
// 의존성:
// 1. IMeleeEnemyBehavior 인터페이스를 구현한 스크립트 추가
// 2. 자식 오브젝트에 인식 판정에 사용할 PlayerDetector 스크립트 추가
// 3. 또 다른 자식 오브젝트에 공격 범위 판정에 사용할 PlayerDetector 스크립트 추가
//
// Hierarchy에서 보면 오브젝트 구조가 아래와 같음:
//
//   melee enemy
//   ㄴ player detector
//   ㄴ attack range detector
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
    [SerializeField] private PlayerDetector playerDetector;
    // 플레이어가 공격 범위 안에 있는지 판단하기 위한 detector.
    // chase -> attack 상태 전환 조건으로 쓰인다.
    [SerializeField] private PlayerDetector attackRangeDetector;

    // 컴포넌트 레퍼런스
    private IMeleeEnemyBehavior meleeEnemyBehavior;
    private FlashEffectOnHit flashEffectOnHit;
    private SpriteRenderer spriteRenderer;

    // IDestructible이 요구하는 스탯
    private CharacterStat hp = new(100f, 0f, 100f);
    private CharacterStat defense = new(10f, 0f);

    // 플레이어가 Chase 상태에서 적의 공격 범위 내에 몇 초나 머물렀는지 측정
    private float playerInAttackRangeTimer = 0f;

    private enum State
    {
        Idle,
        Chase,
        Attack,
        Stagger,
    }
    private State state = State.Idle;

    private void Awake()
    {
        meleeEnemyBehavior = GetComponent<IMeleeEnemyBehavior>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        playerDetector.OnPlayerDetect.AddListener(HandlePlayerDetection);
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

                // 만약 플레이어가 공격 범위에 일정 시간 이상 머무른다면 공격 상태로 전환
                if (attackRangeDetector.IsPlayerInRange)
                {
                    playerInAttackRangeTimer += Time.fixedDeltaTime;

                    if (playerInAttackRangeTimer > 0.5f)
                    {
                        state = State.Attack;
                    }
                }
                else
                {
                    playerInAttackRangeTimer = 0f;
                }
            }
            // 추적 범위를 벗어났다면 초기 위치로 돌아온 뒤 대기 상태로 전환
            else
            {
                // 돌아가는 동안 플레이어를 발견하면 다시 주위에 알려줘야 함
                playerDetector.IsDetectionShared = true;

                bool isReturnComplete = meleeEnemyBehavior.ReturnToInitialPosition();
                if (isReturnComplete)
                {
                    state = State.Idle;
                    meleeEnemyBehavior.StartIdle();
                }
            }
        }
        else if (state == State.Attack)
        {
            // 플레이어가 공격 범위 안에 있고 공격 쿨타임이 지났다면 다음 공격 시행
            if (attackRangeDetector.IsPlayerInRange)
            {
                if (meleeEnemyBehavior.IsMeleeAttackReady())
                {
                    meleeEnemyBehavior.MeleeAttack();
                }
            }
            // 공격 범위를 벗어났다면 다시 Chase 상태로 전환
            else
            {
                state = State.Chase;
            }
        }
    }

    // 플레이어 인식 범위와 공격 가능 범위의 방향을
    // 스프라이트가 바라보는 방향과 일치하도록 조정
    private void UpdateDetectorDirection()
    {
        playerDetector.SetDetectorDirection(spriteRenderer.flipX);
        attackRangeDetector.SetDetectorDirection(spriteRenderer.flipX);
    }

    void HandlePlayerDetection()
    {
        if (state == State.Idle)
        {
            state = State.Chase;

            // TODO: 로그 출력 삭제하고 인식 모션 시작
            Debug.Log("플레이어 인식!");

            // 주변에서 인식 정보를 공유받아 이 함수가 호출된 경우
            // 자신의 PlayerDetector에는 플레이어가 아직 감지되지 않은 상태이므로
            // 나중에 OnPlayerDetect가 한 번 더 실행될 수 있음.
            //
            // ex) 다른 층에 있는 적에게서 플레이어 인식 정보를 공유받은 뒤
            //     플레이어가 자신의 층으로 점프해 올라온 경우
            //
            // 하지만 인식 정보 공유는 최초 발견자만 수행해야 하므로
            // 여기서 PlayerDetector의 정보 공유 옵션을 비활성화 해줘야 함.
            playerDetector.IsDetectionShared = false;
        }
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
        // TODO:
        // 1. 경직 구현 (경직 끝나면 chase 상태로 전환)
        // 2. 아직 플레이어를 인식하지 못한 상태였다면 아래 라인 실행 (인식 & 주변에 인식 정보 공유)
        //    (this as IPlayerDetector).ShareDetectionInfo();
        flashEffectOnHit.StartEffectAsync().Forget();
    }

    void IDestructible.OnDestruction()
    {
        Destroy(gameObject);
    }
}
