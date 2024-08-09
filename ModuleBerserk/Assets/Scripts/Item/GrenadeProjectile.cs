using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GrenadeProjectile : ExplodingProjectileBase
{
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float grenadeArrivalTime = 1f; // 수류탄을 던진 후 목적지까지 도달하는데에 걸리는 시간

    public CharacterStat BaseDamage;

    protected override void OnExplosion(Collision2D other)
    {
        var explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        explosion.GetComponent<Hitbox>().BaseDamage = BaseDamage;
    }

    // grenadeArrivalTime 후에 목적지에 도달하도록 초기 속도를 설정
    public void SetInitialVelocity(Vector2 targetPosition)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        Vector2 displacement = targetPosition - (Vector2)transform.position;
        rb.velocity = new Vector2()
        {
            x = displacement.x / grenadeArrivalTime,
            y = displacement.y / grenadeArrivalTime - rb.gravityScale * Physics2D.gravity.y * grenadeArrivalTime * 0.5f
        };
    }
}
