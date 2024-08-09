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
    [SerializeField] private float grenadeArrivalTime; // 수류탄을 던진 후 목적지까지 도달하는데에 걸리는 시간

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private StatRandomizer cooltimeRandomizer;
    private PlatformerMovement platformerMovement;
    private GameObject player;

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
        GameObject grenade = Instantiate(grenadePrefab, transform.position, Quaternion.identity);
        Rigidbody2D rb = grenade.GetComponent<Rigidbody2D>();

        // 수류탄이 도착해야 할 위치 (플레이어가 서있는 곳)
        Vector2 targetPosition = new Vector2()
        {
            x = player.transform.position.x,
            y = player.GetComponent<Collider2D>().bounds.min.y
        };

        // grenadeArrivalTime 후에 목적지에 도달하도록 초기 속도를 설정
        Vector2 displacement = targetPosition - (Vector2)grenade.transform.position;
        rb.velocity = new Vector2()
        {
            x = displacement.x / grenadeArrivalTime,
            y = displacement.y / grenadeArrivalTime - rb.gravityScale * Physics2D.gravity.y * grenadeArrivalTime * 0.5f
        };
    }

    bool IEnemyAttackBehavior.IsAttackMotionFinished { get; set; } = true;

    bool IEnemyAttackBehavior.IsAttackPossible()
    {
        return attackRange.IsPlayerInRange && attackCooltime <= 0f;
    }

    void IEnemyAttackBehavior.StartAttack()
    {
        (this as IEnemyAttackBehavior).IsAttackMotionFinished = false;

        animator.SetTrigger(attackAnimationTriggerName);

        // 직전에 이동하고 있었을 수 있으므로 확실히 제자리에 정지시킴
        platformerMovement.ApplyHighFriction();

        attackCooltime = delayBetweenAttacks * cooltimeRandomizer.SampleRandomizationFactor();
    }

    void IEnemyAttackBehavior.StopAttack()
    {
        (this as IEnemyAttackBehavior).IsAttackMotionFinished = true;
    }
}
