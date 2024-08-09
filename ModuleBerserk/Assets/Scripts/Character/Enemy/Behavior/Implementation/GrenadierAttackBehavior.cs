using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(StatRandomizer))]
[RequireComponent(typeof(PlatformerMovement))]
public class GrenadierAttackBehavior : MonoBehaviour, IEnemyAttackBehavior
{
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private PlayerDetectionRange attackRange;
    [SerializeField] private float delayBetweenAttacks;
    [SerializeField] private string attackAnimationTriggerName;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private StatRandomizer cooltimeRandomizer;
    private PlatformerMovement platformerMovement;
    private GameObject player;

    // StartAttack()에서 전달받는 공격력 스탯을
    // 수류탄이 생성되기 전까지 잠시 보관하기 위한 변수.
    // 수류탄에 이 스탯을 넘겨주면 그게 폭발 히트박스의 공격력으로 사용되는 방식임.
    private CharacterStat baseDamage;

    private float attackCooltime = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        cooltimeRandomizer = GetComponent<StatRandomizer>();
        platformerMovement = GetComponent<PlatformerMovement>();
        player = GameObject.FindWithTag("Player");
    }

    private void FixedUpdate()
    {
        attackCooltime -= Time.fixedDeltaTime;
        attackRange.SetDetectorDirection(spriteRenderer.flipX);
    }

    // 투척 애니메이션에서 이벤트로 호출되는 함수
    public void ThrowGrenade()
    {
        GrenadeProjectile grenade = Instantiate(grenadePrefab, transform.position, Quaternion.identity).GetComponent<GrenadeProjectile>();

        // 조금 전에 StartAttack()에서 전달받은 공격력 스탯을 투사체에 주입
        grenade.BaseDamage = baseDamage;

        // 수류탄이 도착해야 할 위치 (플레이어가 서있는 곳)
        Vector2 targetPosition = new Vector2()
        {
            x = player.transform.position.x,
            y = player.GetComponent<Collider2D>().bounds.min.y
        };
        grenade.SetInitialVelocity(targetPosition);
    }

    bool IEnemyAttackBehavior.IsAttackMotionFinished { get; set; } = true;

    bool IEnemyAttackBehavior.IsAttackPossible()
    {
        return attackRange.IsPlayerInRange && attackCooltime <= 0f;
    }

    void IEnemyAttackBehavior.StartAttack(CharacterStat baseDamage)
    {
        (this as IEnemyAttackBehavior).IsAttackMotionFinished = false;

        animator.SetTrigger(attackAnimationTriggerName);

        // 직전에 이동하고 있었을 수 있으므로 확실히 제자리에 정지시킴
        platformerMovement.ApplyHighFriction();

        // 잠시 후에 수류탄 오브젝트를 생성하고 공격력 스탯을 넘겨주기 위해 저장
        this.baseDamage = baseDamage;

        attackCooltime = delayBetweenAttacks * cooltimeRandomizer.SampleRandomizationFactor();
    }

    void IEnemyAttackBehavior.StopAttack()
    {
        (this as IEnemyAttackBehavior).IsAttackMotionFinished = true;
    }
}
