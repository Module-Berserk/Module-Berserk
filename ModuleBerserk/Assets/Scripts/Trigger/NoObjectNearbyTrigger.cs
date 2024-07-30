using Cysharp.Threading.Tasks;
using UnityEngine;

// 챕터1 엘리베이터 레버의 사용 조건으로 등장하는 트리거.
// 범위 내에 특정 태그를 보유한 오브젝트가 없는 경우 활성화된다.
public class NoObjectNearbyTrigger : Trigger
{
    // 카운팅할 오브젝트의 태그
    [SerializeField] private string objectTag;
    // 트리거가 OnTriggerEnter와 OnTriggerExit 이벤트에 의해 조절되기 때문에
    // 최초에 범위 안에 아무도 없으면 Activate()가 발생하지 않음.
    //
    // 이 플래그를 체크하면 생성 직후 일정 시간이 지나도 numObjectsWithinRange가
    // 0으로 유지된 경우 최초 1회에 한해 Activate()를 그냥 호출해줌.
    [SerializeField] private bool allowInitialActivation = true;
    [SerializeField] private Color gizmoColor = new Color(0.3f, 0.7f, 1f, 0.2f);

    private int numObjectsWithinRange = 0;

    private void Start()
    {
        if (allowInitialActivation)
        {
            HandleInitialActivationAsync().Forget();
        }
    }

    private async UniTask HandleInitialActivationAsync()
    {
        // 콜라이더 충돌 처리가 확실히 끝날 때까지 잠깐 기다림.
        //
        // Note:
        // 진짜 생성 직후에는 트리거 이벤트가 일어나지 않아서
        // 주위에 뭔가 있는지 없는지 구분할 수가 없다...
        await UniTask.WaitForSeconds(0.1f);

        // 아직도 트리거 접촉이 일어나지 않았으면
        // 초기에 주위에 아무도 없었다는 뜻
        if (numObjectsWithinRange == 0)
        {
            Activate();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(objectTag))
        {
            // 대상이 범위 안에 들어오면 비활성화
            if (numObjectsWithinRange == 0)
            {
                Deactivate();
            }

            numObjectsWithinRange++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(objectTag))
        {
            numObjectsWithinRange--;

            // 모든 대상이 범위에서 나가면 활성화
            if (numObjectsWithinRange == 0)
            {
                Activate();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, GetComponent<BoxCollider2D>().size);
    }
}
