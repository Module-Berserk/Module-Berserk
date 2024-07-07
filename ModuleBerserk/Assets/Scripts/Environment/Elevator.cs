using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

// 활성화되는 순간 꼭대기까지 올라갔다가 다시 돌아오는 엘리베이터.
// 이미 시작된 이동은 취소할 수 없다.
[RequireComponent(typeof(Rigidbody2D))]
public class Elevator : MonoBehaviour
{
    [Header("Initial State")]
    // 아랫층에서 시작하는 경우 true를 주면 되고
    // 윗층에서 시작하는 경우 false를 주면 됨.
    [SerializeField] private bool isInitialPositionDown = true;


    [Header("Movement Setting")]
    // 현재 위치에서 얼마나 멀리 위/아래로 움직일지
    [SerializeField] private float movementRange;
    // 이동에 걸리는 시간 및 속도 커브
    [SerializeField] private float upwardMovementDuration;
    [SerializeField] private float downwardMovementDuration;
    [SerializeField] private Ease movementEase = Ease.InOutSine;
    // 상승을 시작하기 전에 잠깐 기다리며 이펙트를 보여주는 시간
    [SerializeField] private float initialMovementDelay = 1f;
    // 꼭대기에 도달한 뒤 다시 아래로 내려오기 전에 기다리는 시간
    [SerializeField] private float delayBeforeReturning = 3f;
    

    // 이동을 멈추기 위한 목적지와의 거리 조건
    private const float MOVEMENT_STOP_DISTANCE_THRESHOLD = 0.1f;

    private Rigidbody2D rb;
    private float heightUpperBound;
    private float heightLowerBound;
    private bool isActive = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        CalculateMovementBoundary();
    }

    // 엘리베이터의 y축 이동 범위를 계산함
    private void CalculateMovementBoundary()
    {
        // 하강 상태로 시작하는 경우
        if (isInitialPositionDown)
        {
            heightLowerBound = rb.position.y;
            heightUpperBound = rb.position.y + movementRange;
        }
        // 상승 상태로 시작하는 경우
        else
        {
            heightLowerBound = rb.position.y - movementRange;
            heightUpperBound = rb.position.y;
        }
    }

    public void ActivateElevatorDown()
    {
        // 이미 이동 중이라면 요청 무시
        if (isActive)
        {
            return;
        }
        //SFX
        int[] elevatorIndices = {14, 15};
        AudioManager.instance.PlaySFX(elevatorIndices);
        StartOneWayMovementAsync(heightLowerBound, downwardMovementDuration).Forget();
    }

    public void ActivateElevatorUp()
    {
        // 이미 이동 중이라면 요청 무시
        if (isActive)
        {
            return;
        }
        //SFX
        int[] elevatorIndices = {14, 15};
        AudioManager.instance.PlaySFX(elevatorIndices);
        StartOneWayMovementAsync(heightUpperBound, upwardMovementDuration).Forget();
    }

    private async UniTask StartOneWayMovementAsync(float targetHeight, float movementDuration)
    {
        // 엘리베이터 움직임은 비활성화 상태에서만 시작될 수 있음.
        // 중간에 취소하거나 재시작하는 상황이 일어나면 안됨.
        Assert.IsFalse(isActive);

        // 목표 지점에 이미 도착한 상태에서 호출되었다면
        // 어디선가 버그가 발생했을 확률이 높음...
        Assert.AreNotApproximatelyEqual(rb.position.y, targetHeight);

        isActive = true;

        // 1. 이펙트 출력하고 잠시 기다린다
        PlayerElevatorMoveStartEffect();
        await UniTask.WaitForSeconds(initialMovementDelay);

        // 2. 목표 위치로 이동한다
        await MoveToAsync(targetHeight, movementDuration);

        isActive = false;
    }

    // 아래에 있는 상태에서 위로 올라갔다가 다시 내려오는 움직임
    public void ActivateElevatorUpDown()
    {
        // 이미 이동 중이라면 요청 무시
        if (isActive)
        {
            return;
        }

        StartBackAndForthMovementAsync().Forget();
    }

    private async UniTask StartBackAndForthMovementAsync()
    {
        // 엘리베이터 움직임은 비활성화 상태에서만 시작될 수 있음.
        // 중간에 취소하거나 재시작하는 상황이 일어나면 안됨.
        Assert.IsFalse(isActive);

        // 왕복 이동은 엘리베이터가 아래에 있는 상태에서만 호출되어야 함.
        Assert.AreApproximatelyEqual(rb.position.y, heightLowerBound);

        isActive = true;

        // 1. 이펙트 출력하고 잠시 기다린다
        PlayerElevatorMoveStartEffect();
        await UniTask.WaitForSeconds(initialMovementDelay);

        // 2. 위로 이동한다
        await MoveToAsync(heightUpperBound, upwardMovementDuration);

        // 3. 플레이어가 내릴 때까지 잠시 기다린다
        await UniTask.WaitForSeconds(delayBeforeReturning);

        // 4. 다시 원래 자리로 돌아간다
        await MoveToAsync(heightLowerBound, downwardMovementDuration);

        isActive = false;
    }

    private async UniTask MoveToAsync(float destinationHeight, float movementDuration)
    {
        // 목적지 방향으로 일정한 속도 부여
        rb.DOMoveY(destinationHeight, movementDuration)
            .SetEase(movementEase)
            .SetUpdate(UpdateType.Fixed);

        // 이동 속도가 너무 빨라서 목적지를 지나쳐버리지 않는 한
        // 한 프레임씩 기다리며 거리가 충분히 가까워졌는지 체크
        while (IsWithinBoundary())
        {
            float heightDiff = destinationHeight - rb.position.y;
            bool isCloseEnough = Mathf.Abs(heightDiff) < MOVEMENT_STOP_DISTANCE_THRESHOLD;
            if (isCloseEnough)
            {
                break;
            }

            await UniTask.NextFrame();
        }
        
        // 충분히 가까워진 상태이므로 정확한 도착 좌표로 이동
        rb.position = new Vector2(rb.position.x, destinationHeight);
        rb.velocity = Vector2.zero;
    }

    private bool IsWithinBoundary()
    {
        return rb.position.y >= heightLowerBound && rb.position.y <= heightUpperBound;
    }

    private void PlayerElevatorMoveStartEffect()
    {
        // TODO: 이제 곧 엘리베이터 움직인다는 효과 재생 ex) "덜그럭" 하는 효과음, 약간의 진동
        // Debug.Log("엘리베이터가 곧 움직입니다...");
        rb.transform.DOShakePosition(duration: 0.5f, strength: 0.05f, vibrato: 20);
    }
}
