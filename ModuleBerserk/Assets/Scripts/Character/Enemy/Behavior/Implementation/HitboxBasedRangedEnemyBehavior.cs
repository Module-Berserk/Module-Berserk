using UnityEngine;

// 원거리 공격을 하는 적 중에서도 마치 근거리 몹처럼
// 히트박스를 토글하는 방식의 공격을 하는 타입 ex) 샷건
public class HitboxBasedRangedEnemyBehavior : RangedEnemyBehaviorBase
{
    [Header("Ranged Attack")]
    [SerializeField] private float rangedAttackDamage;
    [SerializeField] private ApplyDamageOnContact rangedAttackHitbox;

    private new void Start()
    {
        base.Start();

        rangedAttackHitbox.RawDamage = new CharacterStat(rangedAttackDamage, 0f);
        rangedAttackHitbox.IsHitboxEnabled = false;
    }

    public override void RangedAttack()
    {
        rangedAttackHitbox.SetHitboxDirection(IsFacingLeft);

        base.RangedAttack();
    }

    public void EnableRangedAttackHitbox()
    {
        // 밀쳐내기와 마찬가지로 원거리 공격의 핵심 모션에 약한 경직 저항 부여
        StaggerResistance = StaggerStrength.Weak;
        rangedAttackHitbox.IsHitboxEnabled = true;
    }

    public void DisableRangedAttackHitbox()
    {
        StaggerResistance = StaggerStrength.None;
        rangedAttackHitbox.IsHitboxEnabled = false;
    }

    public override bool TryApplyStagger(AttackInfo attackInfo)
    {
        bool isStaggered = base.TryApplyStagger(attackInfo);

        // 밀쳐내기 공격을 하던 도중에 경직당하면 히트박스가
        // 활성화 상태로 방치될 위험이 있어 꼭 정리해줘야 함.
        if (isStaggered)
        {
            DisableRangedAttackHitbox();
        }

        return isStaggered;
    }

    public override void HandleDeath()
    {
        base.HandleDeath();

        DisableRangedAttackHitbox();
    }
}
