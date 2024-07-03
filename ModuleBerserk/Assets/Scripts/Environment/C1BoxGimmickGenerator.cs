using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class C1BoxGimmickGenerator : MonoBehaviour
{
    [SerializeField] private GameObject c1BoxGimmickPrefab;
    [SerializeField] private Transform boxSpawnPosition;
    // 플레이어가 선반을 공격했을 때 얼마나 크게 진동할 것인지
    [SerializeField] private float shakeStrength = 0.05f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        // 플레이어가 직접 박스 생성기(선반?)을 공격한 경우 약간의 흔들림.
        // 박스의 추락은 C1BoxGimmick에서 처리함
        if (other.transform.parent != null && other.transform.parent.CompareTag("Player"))
        {
            rb.transform.DOShakePosition(1f, shakeStrength);

            // 테스트를 위해 흔들릴 때마다 위에 박스가 없으면 새로 만들어줌
            // TODO: 보스 패턴 완성하면 삭제할 것
            TestGenerateBoxAsync().Forget();
        }
    }

    private async UniTask TestGenerateBoxAsync()
    {
        await UniTask.WaitForSeconds(1f);
        TryGenerateNewBox();
    }
}
