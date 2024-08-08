using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(StatRandomizer))]
[RequireComponent(typeof(PlatformerMovement))]
public class HitboxBasedAttackBehavior : MonoBehaviour, IEnemyAttackBehavior
{
    [SerializeField] private PlayerDetectionRange attackRange;
    [SerializeField] private ApplyDamageOnContact hitbox;
    [SerializeField] private float delayBetweenAttacks;
    [SerializeField] private string attackAnimationTriggerName;

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

        if (hitbox == null)
        {
            throw new ReferenceNotInitializedException("hitbox");
        }
    }

    private void FixedUpdate()
    {
        attackCooltime -= Time.fixedDeltaTime;
        attackRange.SetDetectorDirection(spriteRenderer.flipX);
        hitbox.SetHitboxDirection(spriteRenderer.flipX);
    }

    bool IEnemyAttackBehavior.IsAttackMotionFinished {get; set; }

    bool IEnemyAttackBehavior.IsAttackPossible()
    {
        return attackRange.IsPlayerInRange && attackCooltime <= 0f;
    }

    void IEnemyAttackBehavior.StartAttack()
    {
        (this as IEnemyAttackBehavior).IsAttackMotionFinished = false;

        // 원거리 적이 도주 중에 사용하는 밀치기 공격처럼
        // 현재 방향과 플레이어가 있는 방향이 다른 경우가 있으니
        // 반드시 공격 전에 플레이어를 바라보도록 해야 함.
        spriteRenderer.flipX = player.transform.position.x < transform.position.x;

        animator.SetTrigger(attackAnimationTriggerName);

        // 직전에 이동하고 있었을 수 있으므로 확실히 제자리에 정지시킴
        platformerMovement.ApplyHighFriction();

        attackCooltime = delayBetweenAttacks * cooltimeRandomizer.SampleRandomizationFactor();
    }

    void IEnemyAttackBehavior.StopAttack()
    {
        (this as IEnemyAttackBehavior).IsAttackMotionFinished = true;

        hitbox.IsHitboxEnabled = false;
    }
}
