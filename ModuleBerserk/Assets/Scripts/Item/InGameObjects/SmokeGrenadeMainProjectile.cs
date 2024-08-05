using UnityEngine;

public class SmokeGrenadeMainProjectile : ExplodingProjectileBase
{
    [SerializeField] private GameObject explosionPrefab; // 폭발 이펙트
    [SerializeField] private GameObject smokeAreaPrefab; // 슬로우 장판

    protected override void OnExplosion(Collision2D other)
    {
        Instantiate(explosionPrefab, other.GetContact(0).point, Quaternion.identity);
        Instantiate(smokeAreaPrefab, other.GetContact(0).point, Quaternion.identity);
    }
}
