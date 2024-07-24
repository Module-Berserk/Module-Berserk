using Cysharp.Threading.Tasks;
using UnityEngine;

// 전방에 작은 폭발을 일으키는 콩알탄
public class PopPop : ActiveItemBase
{
    [SerializeField] private GameObject explosionPrefab;

    public override void Use()
    {
        var player = FindObjectOfType<PlayerManager>();

        var explosion = Instantiate(explosionPrefab, ExplosionPosition(player), Quaternion.identity);

        // 플레이어가 바라보는 방향에 따라 위치와 스프라이트 방향을 설정
        var spriteRenderer = explosion.GetComponent<SpriteRenderer>();
        spriteRenderer.flipX = player.IsFacingLeft;
    }

    private Vector2 ExplosionPosition(PlayerManager player)
    {
        // 플레이어 기준 앞으로 던지는 모션에 적합한 기준 위치
        Vector2 position = player.transform.position + Vector3.down * 0.4f;
        position += (player.IsFacingLeft ? Vector2.left : Vector2.right) * 1f;

        return position;
    }
}
