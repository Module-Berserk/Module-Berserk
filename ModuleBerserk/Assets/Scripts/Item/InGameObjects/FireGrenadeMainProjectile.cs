using UnityEngine;

public class FireGrenadeMainProjectile : ExplodingProjectileBase
{
    [SerializeField] private GameObject subprojectilePrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int numSubprojectiles;
    [SerializeField] private float subprojectileSpawnHeight;
    [SerializeField] private float subprojectileMinAngle;
    [SerializeField] private float subprojectileMaxAngle;
    [SerializeField] private float subprojectileInitialSpeed;


    protected override void OnExplosion(Collision2D other)
    {
        // 메인 폭발
        Instantiate(explosionPrefab, other.contacts[0].point, Quaternion.identity);

        // 각각의 subprojectile이 전체 angle min~max를 균등하게
        // 나누고 그 안에서 랜덤 각도를 고르는 방식.
        // ex) 0~90 사이에 2개의 projectile이면 첫 번째는 0~45도 구간 / 두 번째는 45~90도 구간에 생성
        float angleRangePerProjectile = (subprojectileMaxAngle - subprojectileMinAngle) / numSubprojectiles;
        for (int i = 0; i < numSubprojectiles; ++i)
        {
            var projectile = Instantiate(subprojectilePrefab, transform.position + Vector3.up * subprojectileSpawnHeight, Quaternion.identity);
            var rb = projectile.GetComponent<Rigidbody2D>();
            
            var angle = subprojectileMinAngle + angleRangePerProjectile * (i + Random.Range(0.3f, 0.7f));
            var direction = Quaternion.Euler(Vector3.forward * angle);
            rb.velocity = direction * Vector2.right * subprojectileInitialSpeed;
        }
    }
}
