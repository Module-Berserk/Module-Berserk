using UnityEngine;

// 화염 수류탄 등 투척형 아이템이 공통으로 사용하는 클래스.
// projectilePrefab만 다르게 설정해주면 된다.
public class GrenadeThrower : ActiveItemBase
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    public override void Use()
    {
        // 플레이어 위치에서 생성
        PlayerManager player = FindObjectOfType<PlayerManager>();
        var grenade = Instantiate(projectilePrefab);

        // 투척 위치 & 속도 설정
        var rb = grenade.GetComponent<Rigidbody2D>();
        player.ThrowGrenade(rb);
        player.PlayAttack1and2SFX();
    }
}
