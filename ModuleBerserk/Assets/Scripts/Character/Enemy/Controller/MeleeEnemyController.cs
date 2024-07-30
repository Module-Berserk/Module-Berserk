using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

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
[RequireComponent(typeof(ObjectExistenceSceneState))]
public class MeleeEnemyController : MonoBehaviour, IDestructible
{
    [Header("Player Detectors")]
    // 플레이어가 존재한다는걸 인식할 때 사용하는 detector.
    // idle -> chase 상태 전환 조건으로 쓰인다.
    [SerializeField] private PlayerDetectionRange playerDetectionRange;
    // 플레이어가 공격 범위 안에 있는지 판단하기 위한 detector.
    // chase -> attack 상태 전환 조건으로 쓰인다.
    [SerializeField] private PlayerDetectionRange attackRange;


    [Header("Stats")]
    [SerializeField] private float maxHP = 50f;


    [Header("HP Bar")]
    [SerializeField] private GameObject hpBarUI; // 체력바 루트 오브젝트
    [SerializeField] private Slider hpBarSlider;


    // 컴포넌트 레퍼런스
    private IMeleeEnemyBehavior meleeEnemyBehavior;
    private FlashEffectOnHit flashEffectOnHit;
    private SpriteRenderer spriteRenderer;

    // IDestructible이 요구하는 스탯
    private CharacterStat hp;
    private CharacterStat defense = new(10f, 0f);

    private enum State
    {
        Patrol,
        Chase,
        Attack,
        Stagger,
    }
    private State state = State.Patrol;

    private void Awake()
    {
        meleeEnemyBehavior = GetComponent<IMeleeEnemyBehavior>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        hp = new CharacterStat(maxHP, 0f, maxHP);
        hp.OnValueChange.AddListener(UpdateHPBarUI);
    }

    private void UpdateHPBarUI(float diff)
    {
        // Note: 최초 피격 전까지는 체력바가 숨어있음!
        if (!hpBarUI.activeInHierarchy)
        {
            hpBarUI.SetActive(true);
        }

        hpBarSlider.value = hp.CurrentValue / hp.MaxValue;
    }

    private void Start()
    {
        playerDetectionRange.OnPlayerDetect.AddListener(HandlePlayerDetection);

        // 순찰 상태로 시작
        meleeEnemyBehavior.StartPatrol();
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
            // 추적 범위를 벗어났다면 순찰 상태로 전환
            else
            {
                state = State.Patrol;
                meleeEnemyBehavior.StartPatrol();

                // 순찰 도중에 플레이어를 발견하면 다시 주위에 알려줘야 함
                playerDetectionRange.IsDetectionShared = true;
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
                else
                {
                    meleeEnemyBehavior.Idle();
                }
            }
            // 공격 범위를 벗어났고, 아직 공격 모션이 재생 중이지 않다면 다시 Chase 상태로 전환
            else if (meleeEnemyBehavior.IsAttackMotionFinished())
            {
                state = State.Chase;
            }
        }
        else if (state == State.Stagger)
        {
            // 경직이 끝나면 추적 상태로 전환
            if (meleeEnemyBehavior.IsStaggerFinished())
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
        attackRange.SetDetectorDirection(spriteRenderer.flipX);
    }

    public void HandlePlayerDetection()
    {
        // 순찰 중이었다면 순찰을 멈추고 바로 추적을 시작해야 함
        if (state == State.Patrol)
        {
            meleeEnemyBehavior.StopPatrol();

            state = State.Chase;

            // TODO: 로그 출력 삭제하고 인식 모션 시작
            // Debug.Log("플레이어 인식!");
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

    bool IDestructible.OnDamage(AttackInfo attackInfo)
    {
        (this as IDestructible).HandleHPDecrease(attackInfo.damage);

        // 피격 이펙트
        flashEffectOnHit.StartEffectAsync().Forget();

        // 경직 상태에 들어갔다면 잠시 기다렸다가 추적 시작
        if (meleeEnemyBehavior.TryApplyStagger(attackInfo))
        {
            state = State.Stagger;
        }
        // 만약 슈퍼아머로 인해 경직을 무시했다면 바로 추적 시작
        else
        {
            state = State.Chase;
        }

        return true;
    }

    void IDestructible.OnDestruction()
    {
        // 이동중이었을 수도 있으니 멈추고 사망 처리
        meleeEnemyBehavior.Idle();
        meleeEnemyBehavior.HandleDeath();

        // 체력바 숨기기
        hpBarUI.SetActive(false);

        enabled = false;

        // 세이브 데이터에 죽었다고 기록하기
        GetComponent<ObjectExistenceSceneState>().RecordAsDestroyed();
    }
}
