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
        rangedAttackHitbox.IsHitboxEnabled = true;
    }

    public void DisableRangedAttackHitbox()
    {
        rangedAttackHitbox.IsHitboxEnabled = false;
    }

    public override bool TryApplyStagger(StaggerInfo staggerInfo)
    {
        // 밀쳐내기와 마찬가지로 원거리 공격 모션 중 히트박스가 켜진 동안은 슈퍼아머 판정이라 경직 x
        if (rangedAttackHitbox.IsHitboxEnabled)
        {
            return false;
        }

        return base.TryApplyStagger(staggerInfo);
    }
}
