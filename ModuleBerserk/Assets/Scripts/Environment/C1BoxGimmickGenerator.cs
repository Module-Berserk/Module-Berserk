using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class C1BoxGimmickGenerator : MonoBehaviour
{
    [SerializeField] private GameObject c1BoxGimmickPrefab;
    [SerializeField] private Transform boxSpawnPosition;

    private BoxCollider2D boxCollider2D;

    private void Awake()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();

        // TODO: 테스트 끝나면 삭제할 것
        TryGenerateNewBox();
    }

    // 박스가 이미 사용되었다면 새로 생성함.
    // 보스의 돌진 패턴에서 보스가 벽에 충돌하면 호출됨.
    public void TryGenerateNewBox()
    {
        // Note: 아직 사용되지 않은 박스 기믹은 자식 오브젝트로 남아있음!
        if (GetComponentsInChildren<C1BoxGimmick>().Length == 0)
        {
            GameObject box = Instantiate(c1BoxGimmickPrefab, transform);
            box.transform.position = boxSpawnPosition.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어가 직접 박스 생성기(선반?)을 공격한 경우 약간의 흔들림과 함께 박스를 아래로 떨어트림
        if (other.transform.parent != null && other.transform.parent.CompareTag("Player"))
        {
            foreach (var box in GetComponentsInChildren<C1BoxGimmick>())
            {
                box.transform.parent = null;
                Physics2D.IgnoreCollision(box.GetComponent<Collider2D>(), boxCollider2D);
            }
        }
    }
}
