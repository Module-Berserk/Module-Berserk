using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 감지 범위에 들어온 플레이어를 인식해 이벤트를 발생시키고
// 주변의 PlayerDetector에게 인식 정보를 공유하는 기능을 제공함.
//
// 그냥 특정 범위 안에 플레이어가 존재하는지 확인하는 용도로 사용할 수도 있음.
//
// 용례:
// 1. 플레이어가 시야 범위에 들어왔는지 OnPlayerDetect 이벤트로 확인
// 2. 플레이어가 적의 공격 범위에 머무르는지 IsPlayerInRange 값으로 확인
//
// 사용법:
// 1. 적 오브젝트에 자식 오브젝트를 추가한다 (layer = Interactable)
//    - Interactable은 플레이어만 충돌하는 레이어 => 불필요한 물리 연산 최소화
// 2. 자식 오브젝트에 trigger로 설정된 2d 콜라이더를 넣는다
// 3. 마지막으로 이 스크립트를 추가한다
[RequireComponent(typeof(Collider2D))]
public class PlayerDetector : MonoBehaviour
{
    // 인식 공유 범위 (원형 범위의 반지름)
    [SerializeField] float detectionSharingRadius = 0f;

    // 플레이어가 탐지 범위 콜라이더에 들어왔을 때
    // 주변에 있는 PlayerDetector에 감지 정보를 공유할지 결정하는 옵션.
    public bool IsDetectionShared = false;

    // 플레이어가 직접 탐지 범위 콜라이더에 들어오거나
    // 근처의 PlayerDetector의 ShareDetectionInfo()에 의해
    // 플레이어 인식 정보가 공유된 경우 호출되는 이벤트.
    public UnityEvent OnPlayerDetect;

    // 플레이어가 탐지 범위 콜라이더 안에 존재하는지 (인식 정보 공유는 고려하지 않음!)
    public bool IsPlayerInRange {get; private set;}

    // 컴포넌트 레퍼런스
    private Collider2D detectionRange;

    private void Awake()
    {
        detectionRange = GetComponent<Collider2D>();
    }

    public void SetDetectorDirection(bool flipX)
    {
        // BoxCollider2D처럼 대칭적인 범위를 사용한다고 가정하면
        // x축 offset을 반전시키는 것만으로 대칭 이동이 가능함!
        float newOffsetX = Mathf.Abs(detectionRange.offset.x) * (flipX ? -1f : 1f);
        detectionRange.offset = new Vector2(newOffsetX, detectionRange.offset.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = true;

            if (IsDetectionShared)
            {
                ShareDetectionInfo();
            }

            OnPlayerDetect.Invoke();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = false;
        }
    }

    // 주위에 있는 IPlayerDetector에게 정보를 공유해
    // OnPlayerDetect 이벤트가 호출되게 만든다.
    private void ShareDetectionInfo()
    {
        Debug.Log("주변에 인식 정보 공유");
        var colliders = Physics2D.OverlapCircleAll(transform.position, detectionSharingRadius);
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out PlayerDetector detector) && detector != this)
            {
                detector.OnPlayerDetect.Invoke();
            }
        }
    }
}
