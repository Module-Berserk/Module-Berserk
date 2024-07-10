using Cysharp.Threading.Tasks;
using UnityEngine;

// 원거리 공격이 가능한 적들의 공통적인 행동을 제공하는 클래스.
//
// 원거리 공격 방식마다 자식 클래스가 하나씩 있음:
// 1. 투사체 기반 - ProjectileBasedRangedEnemyBehavior
// 2. 히트박스 기반 - HitboxBasedRangedEnemyBehavior
//
// 필요한 애니메이션 이벤트:
// 1. OnAttackMotionEnd() - 원거리 공격과 밀쳐내기 공격 모션의 마지막 프레임 (둘 다 필요)
// 2. EnableRepelAttackHitbox() - 밀쳐내기 판정 시작되는 프레임
// 3. DisableRepelAttackHitbox() - 밀쳐내기 판정 끝나는 프레임
//
// 제공되는 애니메이션 트리거:
// 1. Stagger - 경직 시작
// 2. RepelAttack - 밀쳐내기 공격 시작
// 3. RangedAttack - 원거리 공격 시작
public abstract class RangedEnemyBehaviorBase : EnemyBehaviorBase, IRangedEnemyBehavior
{
    [Header("Attack Delay")]
    // 다음 원거리 공격까지 기다려야 하는 시간
    [SerializeField] private float delayBetweenRangedAttacks = 3f;
    // 도주 중 밀쳐내기를 사용하기 위해 기다려야 하는 시간
    [SerializeField] private float delayBetweenRepelAttacks = 5f;


    [Header("Repel Attack")]
    [SerializeField] private float repelAttackDamage;
    [SerializeField] private ApplyDamageOnContact repelAttackHitbox;

    
    [Header("Run Away")]
    // 최소 사정거리를 확보하기 위해 도주할 때의 이동 속도
    [SerializeField] private float runAwaySpeed = 1f;


    // 현재 대기 애니메이션이 반복 재생된 횟수
    private int idleAnimationRepetitionCount = 0;
    // 다른 대기 애니메이션으로 전환되기 위한 반복 재생 횟수
    private const int IDLE_ANIMATION_CHANGE_THRESHOLD = 6;

    // 원거리 공격 쿨타임 (0이 되면 가능)
    private float remainingRangedAttackCooltime = 0f;
    // 밀쳐내기 쿨타임 (0이 되면 가능)
    private float remainingRepelAttackCooltime = 0f;

    protected void Start()
    {
        repelAttackHitbox.RawDamage = new CharacterStat(repelAttackDamage, 0f);
        repelAttackHitbox.IsHitboxEnabled = false;
    }

    private new void FixedUpdate()
    {
        base.FixedUpdate();

        remainingRangedAttackCooltime -= Time.fixedDeltaTime;
        remainingRepelAttackCooltime -= Time.fixedDeltaTime;
    }

    public void OnAttackMotionEnd()
    {
        isAttackMotionFinished = true;
    }

    // 원거리 공격은 투사체 방식과 히트박스 방식으로 나뉘기 때문에
    // 공통적인 처리만 여기서 하고 자식 클래스에서 override해야 함.
    // * ProjectileBasedRangedEnemyBehavior 참고
    public virtual void RangedAttack()
    {
        animator.SetTrigger("RangedAttack");

        // 추적 혹은 도주 중지
        platformerMovement.ApplyHighFriction();

        // 약간의 랜덤성을 부여한 쿨타임 시작
        // 적들이 동일한 간격으로 공격하는 것을 방지해 조금 더 자연스럽게 느껴지도록 한다
        remainingRangedAttackCooltime = delayBetweenRangedAttacks * SampleRandomizationFactor();

        // 공격 애니메이션 재생 중
        isAttackMotionFinished = false;
    }

    bool IRangedEnemyBehavior.IsRangedAttackReady()
    {
        return remainingRangedAttackCooltime <= 0f;
    }

    // 대기 애니메이션의 마지막 프레임에 호출되는 이벤트
    // TODO: idle 처리는 EnemyBehaviorBase로 옮기기
    public void OnIdleAnimationEnd()
    {
        idleAnimationRepetitionCount++;

        // 일정 횟수 이상 반복되면 다른 대기 애니메이션을 사용하도록 만든다
        if (idleAnimationRepetitionCount > IDLE_ANIMATION_CHANGE_THRESHOLD)
        {
            animator.SetTrigger("ChangeIdleAnimation");
        }
    }

    void IRangedEnemyBehavior.RepelAttack()
    {
        animator.SetTrigger("RepelAttack");

        // 도주 취소
        platformerMovement.ApplyHighFriction();

        // 최소 사정거리를 확보하기 위해 도주하는 상태에는
        // 플레이어를 등지고 있으므로 공격을 위해 뒤로 돌아봐야 함
        IsFacingLeft = player.transform.position.x < transform.position.x;
        repelAttackHitbox.SetHitboxDirection(IsFacingLeft);

        // 약간의 랜덤성을 부여한 쿨타임 시작
        // 적들이 동일한 간격으로 공격하는 것을 방지해 조금 더 자연스럽게 느껴지도록 한다
        remainingRepelAttackCooltime = delayBetweenRepelAttacks * SampleRandomizationFactor();

        // 공격 애니메이션 재생 중
        isAttackMotionFinished = false;
    }

    public void EnableRepelAttackHitbox()
    {
        // 밀쳐내기 공격의 핵심 모션 도중에는 약한 경직 저항 부여
        StaggerResistance = StaggerStrength.Weak;
        repelAttackHitbox.IsHitboxEnabled = true;
    }

    public void DisableRepelAttackHitbox()
    {
        StaggerResistance = StaggerStrength.None;
        repelAttackHitbox.IsHitboxEnabled = false;
    }

    bool IRangedEnemyBehavior.IsRepelAttackReady()
    {
        return remainingRepelAttackCooltime <= 0f;
    }

    bool IRangedEnemyBehavior.IsAttackMotionFinished()
    {
        return isAttackMotionFinished;
    }

    void IRangedEnemyBehavior.RunAway()
    {
        // 플레이어어가 존재하지 않는 경우
        if (!player)
        {
            return;
        }

        // 플레이어와 멀어지는 방향이 낭떠러지인 경우
        float runAwayDirection = transform.position.x - player.transform.position.x;
        if (platformerMovement.IsOnBrink(runAwayDirection))
        {
            platformerMovement.UpdateMoveVelocity(0f);
            platformerMovement.ApplyHighFriction();
            return;
        }

        // 플레이어와 멀어지는 스프라이트 설정
        IsFacingLeft = runAwayDirection < 0f;

        // 도주
        float desiredSpeed = Mathf.Sign(runAwayDirection) * runAwaySpeed;
        platformerMovement.UpdateMoveVelocity(desiredSpeed);
        platformerMovement.UpdateFriction(desiredSpeed);
    }
}
