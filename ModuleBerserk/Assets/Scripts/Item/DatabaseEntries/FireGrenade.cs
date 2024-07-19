using UnityEngine;

public class FireGrenade : ActiveItemBase
{
    [Header("Projectile")]
    [SerializeField] private GameObject mainProjectilePrefab;

    public override void Use()
    {
        // 플레이어 위치에서 생성
        PlayerManager player = FindObjectOfType<PlayerManager>();
        var grenade = Instantiate(mainProjectilePrefab);

        // 투척 위치 & 속도 설정
        var rb = grenade.GetComponent<Rigidbody2D>();
        player.ThrowGrenade(rb);
        player.PlayAttack1and2SFX();
    }
}
