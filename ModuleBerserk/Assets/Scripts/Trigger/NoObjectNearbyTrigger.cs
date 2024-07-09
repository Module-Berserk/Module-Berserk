using UnityEngine;

// 챕터1 엘리베이터 레버의 사용 조건으로 등장하는 트리거.
// 범위 내에 특정 태그를 보유한 오브젝트가 없는 경우 활성화된다.
public class NoObjectNearbyTrigger : Trigger
{
    // 카운팅할 오브젝트의 태그
    [SerializeField] private string objectTag;
    // 트리거가 OnTriggerEnter와 OnTriggerExit 이벤트에 의해 조절되기 때문에
    // 최초에 범위 안에 아무도 없으면 Activate()가 발생하지 않음.
    // 이 경우 수동으로 플래그를 설정해서 생성되자마자 활성화 상태로 시작하도록 만들어야 함.
    [SerializeField] private bool isInitiallyActive;

    private int numObjectsWithinRange = 0;

    private void Start()
    {
        if (isInitiallyActive)
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
}
