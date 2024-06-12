using UnityEngine;

// 원거리 공격을 하는 적 중에서도 총알같은 투사체를 발사하는 타입.
public class ProjectileBasedRangedEnemyBehavior : RangedEnemyBehaviorBase
{
    [Header("Ranged Attack Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    public override void RangedAttack()
    {
        base.RangedAttack();

        // TODO: 투사체 instantiate하기
    }
}
