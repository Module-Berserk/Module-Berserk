using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

// 원거리 공격을 하는 잡몹의 행동 패턴을 정의하는 클래스.
[RequireComponent(typeof(IRangedEnemyBehavior))]
[RequireComponent(typeof(FlashEffectOnHit))]
public class RangedEnemyController : MonoBehaviour, IDestructible
{
    // 적의 본체인 $를 기준으로 detector들이 아래와 같이 배치되어야 함:
    //
    //    $----------- playerDetectionRange: 플레이어 감지 범위; idle -> chase 상태 전환 조건
    //    $   ---      rangedAttackRange: 원거리 공격의 최소/최대 사정거리; chase -> attack 상태 전환 조건
    // ---$---         runAwayRange: 원거리 공격의 최소 사정거리 이내; chase -> runaway 상태 전환 조건
    //   -$-           repelAttackRange: 도주 중에 밀쳐내기를 시도할 범위
    //
    // runAwayRange와 repelAttackRange는 적이 플레이어를 등진 상태에서도 사용되므로 대칭적인 모양이 필요함.
    [Header("Player Detectors")]
    [SerializeField] private PlayerDetectionRange playerDetectionRange;
    [SerializeField] private PlayerDetectionRange rangedAttackRange;
    [SerializeField] private PlayerDetectionRange runAwayRange;
    [SerializeField] private PlayerDetectionRange repelAttackRange;

    // 컴포넌트 레퍼런스
    private IRangedEnemyBehavior rangedEnemyBehavior;
    private FlashEffectOnHit flashEffectOnHit;
    private SpriteRenderer spriteRenderer;

    // IDestructible이 요구하는 스탯
    private CharacterStat hp = new(100f, 0f, 100f);
    private CharacterStat defense = new(10f, 0f);

    private enum State
    {
        Patrol,
        Chase,
        Attack,
        Stagger,
        RunAway,
    }
    private State state = State.Patrol;

    private void Awake()
    {
        rangedEnemyBehavior = GetComponent<IRangedEnemyBehavior>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        playerDetectionRange.OnPlayerDetect.AddListener(HandlePlayerDetection);

        // 순찰 상태로 시작
        rangedEnemyBehavior.StartPatrol();
    }

    private void FixedUpdate()
    {
        UpdateDetectorDirection();
        
        // 공격 모션을 재생하는 도중에는 추가적인 행동을 하지 않음
        if (!rangedEnemyBehavior.IsAttackMotionFinished())
        {
            return;
        }

        if (state == State.Chase)
        {
            // 추적 가능한 범위에 있다면 플레이어에게 접근
            if (rangedEnemyBehavior.CanChasePlayer())
            {
                // 플레이어가 공격 범위에 들어오면 즉시 공격 시도
                if (rangedAttackRange.IsPlayerInRange)
                {
                    state = State.Attack;
                }
                // 플레이어가 바로 최소 사정거리 안으로 들어온 경우 도주 상태로 전환
                else if (repelAttackRange.IsPlayerInRange)
                {
                    state = State.RunAway;
                }
                else
                {
                    rangedEnemyBehavior.Chase();
                }
            }
            // 추적 범위를 벗어났다면 순찰 상태로 전환
            else
            {
                state = State.Patrol;
                rangedEnemyBehavior.StartPatrol();

                // 순찰 도중에 플레이어를 발견하면 다시 주위에 알려줘야 함
                playerDetectionRange.IsDetectionShared = true;
            }
        }
        else if (state == State.Attack)
        {
            // 플레이어가 공격 범위 안에 있고 공격 쿨타임이 지났다면 다음 공격 시행
            if (rangedAttackRange.IsPlayerInRange)
            {
                if (rangedEnemyBehavior.IsRangedAttackReady())
                {
                    rangedEnemyBehavior.RangedAttack();
                }
            }
            // 플레이어가 너무 멀거나 가까워서 공격 범위를 벗어난 경우
            else
            {
                // 최소 사정거리 안으로 플레이어가 들어온 경우 도주 상태로 전환
                if (runAwayRange.IsPlayerInRange)
                {
                    state = State.RunAway;
                }
                // 플레이어가 아예 멀리 떨어진 경우 추적 시작
                else
                {
                    state = State.Chase;
                }
            }
        }
        else if (state == State.Stagger)
        {
            // 경직이 끝나면 추적 상태로 전환
            if (rangedEnemyBehavior.IsStaggerFinished())
            {
                state = State.Chase;
            }
        }
        else if (state == State.RunAway)
        {
            // 아직 플레이어가 최소 사정거리 안에 있는 경우
            if (runAwayRange.IsPlayerInRange)
            {
                // 밀쳐내기가 가능하다면 시도
                if (repelAttackRange.IsPlayerInRange && rangedEnemyBehavior.IsRepelAttackReady())
                {
                    rangedEnemyBehavior.RepelAttack();
                }
                // 쿨타임 중이라면 최소 사정거리 확보를 위해 도주
                else
                {
                    rangedEnemyBehavior.RunAway();
                }
            }
            // 최소 사정거리를 벗어났다면 다시 추격부터 시작
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
        playerDetectionRange.SetDetectorDirection(spriteRenderer.flipX);
        rangedAttackRange.SetDetectorDirection(spriteRenderer.flipX);
    }

    void HandlePlayerDetection()
    {
        // 순찰 중이었다면 순찰을 멈추고 바로 추적을 시작해야 함
        if (state == State.Patrol)
        {
            rangedEnemyBehavior.StopPatrol();

            state = State.Chase;

            // TODO: 로그 출력 삭제하고 인식 모션 시작
            // 인식 모션의 출력이나 모션 도중에 잠깐 멈춰서있는 것은
            // 모든 잡몹이 공유하는 패턴이므로 IEnemyBehavior에 넣을 것
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
        (this as IDestructible).HandleHPDecrease(finalDamage);
        
        // 피격 이펙트
        flashEffectOnHit.StartEffectAsync().Forget();

        // 경직 상태에 들어갔다면 잠시 기다렸다가 추적 시작
        if (rangedEnemyBehavior.TryApplyStagger(staggerInfo))
        {
            state = State.Stagger;
        }
    }

    void IDestructible.OnDestruction()
    {
        Destroy(gameObject);
    }
}
