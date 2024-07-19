using UnityEngine;

public class FireGrenadeMainProjectile : MonoBehaviour
{
    [SerializeField] private GameObject subprojectilePrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int numSubprojectiles;
    [SerializeField] private float subprojectileSpawnHeight;
    [SerializeField] private float subprojectileMinAngle;
    [SerializeField] private float subprojectileMaxAngle;
    [SerializeField] private float subprojectileInitialSpeed;

    private bool isProjectileSpawned = false;

    private void OnCollisionStay2D(Collision2D other)
    {
        // 바닥이 아니라 벽/천장에는 반응하지 않음
        if (Vector2.Dot(other.contacts[0].normal, Vector2.up) < 0.1f)
        {
            return;
        }

        // 경사로에서 둘 이상의 콜라이더에 동시에 충돌하는
        // 상황에서도 한 번만 파편을 생성하도록 제한함.
        if (!isProjectileSpawned)
        {
            isProjectileSpawned = true;

            // 메인 폭발
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

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

            Destroy(gameObject);

        }
    }
}
