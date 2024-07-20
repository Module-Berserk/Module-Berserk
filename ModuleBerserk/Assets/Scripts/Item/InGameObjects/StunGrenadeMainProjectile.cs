using UnityEngine;

public class StunGrenadeMainProjectile : ExplodingProjectileBase
{
    [SerializeField] private GameObject explosionPrefab; // 폭발 이펙트 & 기절 부여

    protected override void OnExplosion(Collision2D other)
    {
        Instantiate(explosionPrefab, other.contacts[0].point, Quaternion.identity);
    }
}
