using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 감지 범위에 들어온 플레이어를 인식해 이벤트를 발생시키고
// 주변의 PlayerDetectionRange에게 인식 정보를 공유하는 기능을 제공함.
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
public class PlayerDetectionRange : MonoBehaviour
{
    [Header("Detection")]
    // 인식 공유 범위 (원형 범위의 반지름)
    [SerializeField] private float detectionSharingRadius = 0f;
    // 인식 또는 인식 공유를 막을 레이어 (ex. ground)
    [SerializeField] private LayerMask detectionBlockingLayerMask;
    // 인식 또는 인식 공유를 장애물이 막아야 하는지 결정하는 옵션.
    // true라면 자신과 대상 사이에 raycast가 추가적으로 수행된다.
    // 근접 공격 가능 범위처럼 굳이 raycast까지 할 필요가 없는 경우
    // false를 줘서 약간의 성능 향상을 노릴 수 있음.
    [SerializeField] private bool shouldConsiderLineOfSight = true;

    // 플레이어가 탐지 범위 콜라이더에 들어왔을 때
    // 주변에 있는 PlayerDetectionRange에 감지 정보를 공유할지 결정하는 옵션.
    public bool IsDetectionShared = false;

    // 플레이어가 직접 탐지 범위 콜라이더에 들어오거나
    // 근처의 PlayerDetectionRange의 ShareDetectionInfo()에 의해
    // 플레이어 인식 정보가 공유된 경우 호출되는 이벤트.
    public UnityEvent OnPlayerDetect;

    // 플레이어가 탐지 범위 콜라이더 안에 존재하는지 (인식 정보 공유는 고려하지 않음!)
    public bool IsPlayerInRange {get; private set;}
    // 플레이어를 식별 가능한지
    //
    // IsPlayerInRange가 true여도 벽 등에 시선이 가로막히면
    // IsPlayerDetected는 false가 될 수 있음.
    public bool IsPlayerDetected {get; private set;}

    // 컴포넌트 레퍼런스
    private Collider2D detectionRange;

    // 플레이어가 영역 안에 있을 때 시선이 닿는지 테스트하기 위해 참조.
    // 영역 안에 있어도 벽에 가로막히면 detected로 취급하지 않는다!
    private GameObject player;

    private void Awake()
    {
        detectionRange = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (shouldConsiderLineOfSight && IsPlayerInRange)
        {
            // 플레이어가 영역 안에는 들어왔지만 아직 시야가 가려있는 상태라면
            // 이번 프레임에는 플레이어가 시야에 들어왔는지 확인함
            if (!IsPlayerDetected && IsTargetVisible(player))
            {
                HandlePlayerDetection();
            }
            // 플레이어가 영역 안에 있고 시야에도 들어온 상태라면
            // 이번 프레임에 플레이어가 벽 뒤로 숨었는지 확인함
            else if (IsPlayerDetected && !IsTargetVisible(player))
            {
                IsPlayerDetected = false;
            }
        }
    }

    private void HandlePlayerDetection()
    {
        if (IsDetectionShared)
        {
            ShareDetectionInfo();
        }

        OnPlayerDetect.Invoke();
    }

    private bool IsTargetVisible(GameObject target)
    {
        // 시야가 벽에 가로막혔는지 체크
        Vector2 origin = transform.position;
        Vector2 direction = target.transform.position - transform.position;
        float distance = direction.magnitude;
        if (Physics2D.Raycast(origin, direction, distance, detectionBlockingLayerMask))
        {
            return false;
        }

        return true;
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
            player = other.gameObject;

            IsPlayerInRange = true;

            // 시야가 가리는 것을 고려할 필요가 없다면 바로 detected로 처리
            if (!shouldConsiderLineOfSight) {
                HandlePlayerDetection();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = false;
            IsPlayerDetected = false;
        }
    }

    // 주위에 있는 PlayerDetectionRange에게 정보를 공유해
    // OnPlayerDetect 이벤트가 호출되게 만든다.
    private void ShareDetectionInfo()
    {
        Debug.Log("주변에 인식 정보 공유");
        var colliders = Physics2D.OverlapCircleAll(transform.position, detectionSharingRadius);
        foreach (var collider in colliders)
        {
            // 공유 범위 안에 존재하는 PlayerDetectionRange 중에서 시야가 확보된 대상에 한해 정보 공유.
            // 플랫폼, 벽 등으로 단절된 공간에 정보를 공유하는 상황을 막아준다.
            if (collider.TryGetComponent(out PlayerDetectionRange detector) && detector != this)
            {
                if (IsTargetVisible(collider.gameObject))
                {
                    detector.OnPlayerDetect.Invoke();
                }
            }
        }
    }
}
