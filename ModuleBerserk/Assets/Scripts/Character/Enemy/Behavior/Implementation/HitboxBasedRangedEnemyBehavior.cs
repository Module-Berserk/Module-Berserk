using UnityEngine;

// 원거리 공격을 하는 적 중에서도 마치 근거리 몹처럼
// 히트박스를 토글하는 방식의 공격을 하는 타입 ex) 샷건
public class HitboxBasedRangedEnemyBehavior : RangedEnemyBehaviorBase
{
    public override void RangedAttack()
    {
        hitboxes.SetHitboxDirection(IsFacingLeft);

        base.RangedAttack();
    }
}
