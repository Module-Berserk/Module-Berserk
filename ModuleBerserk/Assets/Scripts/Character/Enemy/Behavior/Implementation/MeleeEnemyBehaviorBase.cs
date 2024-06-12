using Cysharp.Threading.Tasks;
using UnityEngine;

// 근접 공격만 가능한 적의 행동을 제공하는 클래스.
// 원거리 적과 다르게 공격 모션의 시작부터 후딜레이 직전까지 슈퍼아머가 부여된다.
//
// 필요한 애니메이션 이벤트:
// 1. OnAttackMotionEnd() - 공격 모션의 마지막 프레임
// 2. EnableMeleeAttackHitbox() - 공격 판정 시작되는 프레임
// 3. DisableMeleeAttackHitbox() - 공격 판정 끝나는 프레임
// 4. DisableSuperArmor() - 공격 모션의 마지막 후딜레이가 시작되는 프레임
//
// 제공되는 애니메이션 트리거:
// 1. Stagger - 경직 시작
// 2. MeleeAttack - 공격 모션 시작
public class MeleeEnemyBehaviorBase : EnemyBehaviorBase, IMeleeEnemyBehavior
{
    [Header("Melee Attack")]
    [SerializeField] private float meleeAttackDamage;
    // 다음 공격까지 기다려야 하는 시간
    [SerializeField] private float delayBetweenMeleeAttacks = 3f;
    [SerializeField] private ApplyDamageOnContact meleeAttackHitbox;


    // 현재 대기 애니메이션이 반복 재생된 횟수
    private int idleAnimationRepetitionCount = 0;
    // 다른 대기 애니메이션으로 전환되기 위한 반복 재생 횟수
    private const int IDLE_ANIMATION_CHANGE_THRESHOLD = 6;

    // 근접 공격 쿨타임 (0이 되면 공격 가능)
    private float remainingMeleeAttackCooltime = 0f;

    // 지금 공격 애니메이션이 재생 중인지 확인하기 위한 플래그
    private bool isAttackMotionFinished = true;
    // 공격 모션의 선딜 ~ 타격까지 슈퍼아머를 부여하는 플래그.
    // MeleeAttack()에서 true로 설정된 뒤
    // 애니메이션 이벤트 DisableSuperArmor()에서 false로 돌아옴.
    private bool isSuperArmorEnabled = false;

    private void Start()
    {
        meleeAttackHitbox.RawDamage = new CharacterStat(meleeAttackDamage, 0f);
        meleeAttackHitbox.IsHitboxEnabled = false;
    }

    private new void FixedUpdate()
    {
        base.FixedUpdate();

        remainingMeleeAttackCooltime -= Time.fixedDeltaTime;
    }

    public void OnAttackMotionEnd()
    {
        isAttackMotionFinished = true;
    }

    public override bool TryApplyStagger(StaggerInfo staggerInfo)
    {
        // 슈퍼아머 판정인 경우 경직 x
        if (isSuperArmorEnabled)
        {
            return false;
        }

        // 공격 모션 후딜레이가 재생 중이었을 가능성이 있으니 안전하게 플래그 정리.
        isAttackMotionFinished = true;

        // 애니메이션 재생
        animator.SetTrigger("Stagger");

        GetStaggeredForDuration(staggerInfo).Forget();

        return true;
    }

    void IMeleeEnemyBehavior.MeleeAttack()
    {
        // 바라보는 방향으로 히트박스 이동
        meleeAttackHitbox.SetHitboxDirection(IsFacingLeft);

        animator.SetTrigger("MeleeAttack");

        // 약간의 랜덤성을 부여한 쿨타임 시작
        // 적들이 동일한 간격으로 공격하는 것을 방지해 조금 더 자연스럽게 느껴지도록 한다
        remainingMeleeAttackCooltime = delayBetweenMeleeAttacks * SampleRandomizationFactor();

        // 공격 애니메이션 재생 중
        isAttackMotionFinished = false;

        // 후딜레이 전까지는 슈퍼아머 판정
        isSuperArmorEnabled = true;
    }

    bool IMeleeEnemyBehavior.IsMeleeAttackReady()
    {
        return remainingMeleeAttackCooltime <= 0f;
    }

    bool IMeleeEnemyBehavior.IsAttackMotionFinished()
    {
        return isAttackMotionFinished;
    }

    public void EnableMeleeAttackHitbox()
    {
        meleeAttackHitbox.IsHitboxEnabled = true;
    }

    public void DisableMeleeAttackHitbox()
    {
        meleeAttackHitbox.IsHitboxEnabled = false;
    }

    public void DisableSuperArmor()
    {
        isSuperArmorEnabled = false;
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
}
