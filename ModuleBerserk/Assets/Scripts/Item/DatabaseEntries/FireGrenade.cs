using UnityEngine;

public class FireGrenade : ActiveItemBase
{
    [Header("Projectile")]
    [SerializeField] private GameObject mainProjectilePrefab;
    [SerializeField] private float projectileSpeed;

    public override void Use()
    {
        // 플레이어 위치에서 생성
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        var projectile = Instantiate(mainProjectilePrefab, player.transform.position, Quaternion.identity);

        // TODO: 던지는 방향 설정
        var rb = projectile.GetComponent<Rigidbody2D>();
        rb.velocity = new Vector2(-1f, 1f) * projectileSpeed;


        // 주인공과는 충돌하지 않도록 설정
        Physics2D.IgnoreCollision(projectile.GetComponent<Collider2D>(), player.GetComponent<Collider2D>());
    }
}
