using UnityEngine;

public class FireGrenadeMainProjectile : MonoBehaviour
{
    [SerializeField] private GameObject subprojectilePrefab;
    [SerializeField] private int numSubprojectiles;
    [SerializeField] private int subprojectileSpawnHeight;
    [SerializeField] private int subprojectileMinAngle;
    [SerializeField] private int subprojectileMaxAngle;
    [SerializeField] private int subprojectileInitialSpeed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 각각의 subprojectile이 전체 angle min~max를 균등하게
        // 나누고 그 안에서 랜덤 각도를 고르는 방식.
        // ex) 0~90 사이에 2개의 projectile이면 첫 번째는 0~45도 구간 / 두 번째는 45~90도 구간에 생성
        float angleRangePerProjectile = (subprojectileMaxAngle - subprojectileMinAngle) / numSubprojectiles;
        for (int i = 0; i < numSubprojectiles; ++i)
        {
            var projectile = Instantiate(subprojectilePrefab, transform.position + Vector3.up * subprojectileSpawnHeight, Quaternion.identity);
            var rb = projectile.GetComponent<Rigidbody2D>();
            
            var angle = angleRangePerProjectile * i + Random.Range(0f, angleRangePerProjectile);
            var direction = Quaternion.Euler(Vector3.forward * angle);
            rb.velocity = direction * Vector2.right * subprojectileInitialSpeed;
        }

        Destroy(gameObject);
    }
}
