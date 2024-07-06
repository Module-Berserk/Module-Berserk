using UnityEngine;
using UnityEngine.Assertions;

// 챕터1 보스의 박격포 패턴 중 포탄 하나의 폭발 애니메이션 및 히트박스 처리 담당
public class C1BossCannonExplode : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float damage;

    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        GetComponent<ApplyDamageOnContact>().RawDamage = new CharacterStat(damage);

        AlignPositionToGround();
    }

    // 착탄 지점이 정확히 지면 위에 오도록 y축 좌표를 조정함.
    // 포탄 오브젝트를 생성하는 쪽에서 정확한 y좌표를 맞추기 어려울 수 있어서
    // 대충 땅 위에 생성하면 이렇게 보정해주는 방식으로 구현함.
    private void AlignPositionToGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, groundLayer);
        Assert.IsNotNull(hit.collider);

        // 지표면으로부터 중심 위치를 콜라이더 높이의 절반만큼 올리면
        // 콜라이더가 딱 지면과 접하는 상황을 만들 수 있음
        float newY = hit.point.y + boxCollider.size.y / 2f;
        transform.position = new Vector2(transform.position.x, newY);
    }

    // 폭발 애니메이션이 끝나면 호출되는 함수
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
