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
    private float attackCooltime = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        cooltimeRandomizer = GetComponent<StatRandomizer>();
        platformerMovement = GetComponent<PlatformerMovement>();

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

        animator.SetTrigger(attackAnimationTriggerName);

        platformerMovement.ApplyHighFriction();

        attackCooltime = delayBetweenAttacks * cooltimeRandomizer.SampleRandomizationFactor();
    }

    void IEnemyAttackBehavior.StopAttack()
    {
        (this as IEnemyAttackBehavior).IsAttackMotionFinished = true;

        hitbox.IsHitboxEnabled = false;
    }
}
