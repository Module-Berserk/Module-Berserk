using UnityEngine;

public class GrenadeProjectile : ExplodingProjectileBase
{
    [SerializeField] private GameObject explosionPrefab;

    protected override void OnExplosion(Collision2D other)
    {
        Instantiate(explosionPrefab, other.GetContact(0).point, Quaternion.identity);
    }
}
