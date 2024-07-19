using UnityEngine;

public class FireGrenade : ActiveItemBase
{
    [Header("Projectile")]
    [SerializeField] private GameObject mainProjectilePrefab;

    public override void Use()
    {
        // 플레이어 위치에서 생성
        PlayerManager player = FindObjectOfType<PlayerManager>();
        var grenade = Instantiate(mainProjectilePrefab, player.transform.position, Quaternion.identity);

        // TODO: 던지는 방향 설정
        var rb = grenade.GetComponent<Rigidbody2D>();
        player.ThrowGrenade(rb);
    }
}