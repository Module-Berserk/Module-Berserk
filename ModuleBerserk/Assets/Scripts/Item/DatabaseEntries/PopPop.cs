using UnityEngine;

// 전방에 작은 폭발을 일으키는 콩알탄
public class PopPop : ActiveItemBase
{
    [SerializeField] private GameObject explosionPrefab;

    public override void Use()
    {
        var player = FindObjectOfType<PlayerManager>();
        var explosion = Instantiate(explosionPrefab, player.transform.position + Vector3.down * 0.4f, Quaternion.identity);

        // 플레이어가 바라보는 방향에 따라 위치와 스프라이트 방향을 설정
        var spriteRenderer = explosion.GetComponent<SpriteRenderer>();
        spriteRenderer.flipX = player.IsFacingLeft;

        explosion.transform.position += (player.IsFacingLeft ? Vector3.left : Vector3.right) * 1f;
    }
}
