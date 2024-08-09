using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(IEnemyPatrolBehavior))]
[RequireComponent(typeof(IEnemyChaseBehavior))]
[RequireComponent(typeof(IEnemyStaggerBehavior))]
[RequireComponent(typeof(IEnemyAttackBehavior))]
[RequireComponent(typeof(PlatformerMovement))]
[RequireComponent(typeof(FlashEffectOnHit))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ObjectExistenceSceneState))]
public class EnemyController : MonoBehaviour, IDestructible
{
    [Header("Stats")]
    [SerializeField] private float baseHP = 50f;
    [SerializeField] private float baseDefense = 10f;
    [SerializeField] private float baseDamage = 10f;
    

    [Header("HP Bar")]
    [SerializeField] private GameObject hpBarUI; // 체력바 루트 오브젝트
    [SerializeField] private Slider hpBarSlider;


    [Header("Player Detection")]
    [SerializeField] private PlayerDetectionRange playerDetectionRange;


    [Header("Debug")]
    [SerializeField] private bool logCurrentState = false;


    private IEnemyPatrolBehavior patrolBehavior;
    private IEnemyChaseBehavior chaseBehavior;
    private IEnemyStaggerBehavior staggerBehavior;
    private IEnemyAttackBehavior[] attackBehaviors; // 가능한 모든 공격 패턴
    private IEnemyAttackBehavior activeAttackBehavior = null; // 지금 진행중인 공격 패턴 (attackBehaviors 중 하나)

    private PlatformerMovement platformerMovement;
    private FlashEffectOnHit flashEffectOnHit;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D rb;

    private CharacterStat hp;
    private CharacterStat defense;
    private CharacterStat damage;

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
        if (playerDetectionRange == null)
        {
            throw new ReferenceNotInitializedException("playerDetectionRange");
        }

        chaseBehavior = GetComponent<IEnemyChaseBehavior>();
        patrolBehavior = GetComponent<IEnemyPatrolBehavior>();
        staggerBehavior = GetComponent<IEnemyStaggerBehavior>();
        attackBehaviors = GetComponents<IEnemyAttackBehavior>();

        platformerMovement = GetComponent<PlatformerMovement>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        hp = new CharacterStat(baseHP, 0f, baseHP);
        defense = new CharacterStat(baseDefense, 0f);
        damage = new CharacterStat(baseDamage, 0f);

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
        patrolBehavior.StartPatrol();
    }

    public void HandlePlayerDetection()
    {
        // 순찰 중이었다면 순찰을 멈추고 바로 추적을 시작해야 함.
        // 다만 행동 반경 제한 등으로 인해 추적이 불가능한 상황에서
        // 플레이어를 인식한 경우에는 순찰 상태를 유지함.
        if (state == State.Patrol && chaseBehavior.CanChasePlayer())
        {
            patrolBehavior.StopPatrol();

            state = State.Chase;
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

    private void FixedUpdate()
    {
        if (logCurrentState)
        {
            Debug.Log($"[EnemyController] State: {state}", gameObject);
        }

        platformerMovement.HandleGroundContact();
        playerDetectionRange.SetDetectorDirection(spriteRenderer.flipX);
        animator.SetBool("IsMoving", Mathf.Abs(rb.velocity.x) > 0.01f);

        if (state == State.Stagger)
        {
            // 경직이 끝나면 무조건 추적 상태로 전환
            if (!staggerBehavior.IsStaggered)
            {
                state = State.Chase;
            }
        }
        else if (state == State.Chase)
        {
            // 추적 가능한 범위에 있다면 플레이어에게 접근
            if (chaseBehavior.CanChasePlayer())
            {
                chaseBehavior.ChasePlayer();

                // 여러가지 공격 방식 중에 지금 준비된 공격이 있는지 확인.
                // 실행 가능한 공격이 있다면 바로 Attack 상태로 전환함.
                foreach (var attackBehavior in attackBehaviors)
                {
                    if (attackBehavior.IsAttackPossible())
                    {
                        attackBehavior.StartAttack(damage);

                        activeAttackBehavior = attackBehavior;
                        state = State.Attack;
                    }
                }
            }
            // 추적 범위를 벗어났다면 순찰 상태로 전환
            else
            {
                state = State.Patrol;
                patrolBehavior.StartPatrol();

                playerDetectionRange.IsDetectionShared = true;
            }
        }
        else if (state == State.Attack)
        {
            // 이번 공격이 끝날 때까지 기다렸다가 다시 추적 상태로 전환.
            if (activeAttackBehavior.IsAttackMotionFinished)
            {
                state = State.Chase;
            }
        }
    }

    // 공격 애니메이션의 마지막 프레임에 호출되는 이벤트.
    // 애니메이션이 완전히 끝났다는 것을 알려주며,
    // FSM에서는 Attack -> Chase 전환 조건으로서 작동한다.
    //
    // 경직, 사망 모션 등 히트박스를 정리해야 하는 곳에서도 사용됨.
    public void StopActiveAttack()
    {
        if (activeAttackBehavior != null)
        {
            activeAttackBehavior.StopAttack();
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

    bool IDestructible.OnDamage(AttackInfo attackInfo)
    {
        (this as IDestructible).HandleHPDecrease(attackInfo.damage);

        // 피격 이펙트
        flashEffectOnHit.StartEffectAsync().Forget();

        // 경직 상태에 들어갔다면 잠시 기다렸다가 추적 시작
        if (staggerBehavior.TryApplyStagger(attackInfo))
        {
            state = State.Stagger;

            // patrolBehavior는 stateful해서 여기서 명시적으로 멈춰주지 않으면
            // 순찰 패턴 중 "대기" 상태를 처리하는 코드에 의해
            // 넉백 효과가 의도한 것보다 빨리 사라질 수 있음!
            patrolBehavior.StopPatrol();
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
        platformerMovement.ApplyHighFriction();

        // 체력바 숨기기
        hpBarUI.SetActive(false);

        // 컨트롤러 비활성화
        enabled = false;

        animator.SetTrigger("Die");

        // 세이브 데이터에 죽었다고 기록하기
        GetComponent<ObjectExistenceSceneState>().RecordAsDestroyed();
    }

    // 효과음 함수들은 언젠가 별도의 스크립트로 분리할 예정...
    private void PlayReloadSFX() {
        int[] reloadIndices = {16};
        AudioManager.instance.PlaySFXBasedOnPlayer(reloadIndices, this.transform);
    }
    private void PlayShotGunSFX() {
        int[] shotIndices = {17};
        AudioManager.instance.PlaySFXBasedOnPlayer(shotIndices, this.transform);
    }

    private void PlayVictimSFX() {
        int[] victimIndices = {41, 42, 43, 44, 45};
        AudioManager.instance.PlaySFXBasedOnPlayer(victimIndices, this.transform);
    }
}
