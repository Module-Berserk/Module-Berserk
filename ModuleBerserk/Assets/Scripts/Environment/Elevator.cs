using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;

// 활성화되는 순간 꼭대기까지 올라갔다가 다시 돌아오는 엘리베이터.
// 이미 시작된 이동은 취소할 수 없다.
//
// TODO: 현재 엘리베이터의 가속이 너무 빠르면 아래로 내려갈 때
// 플레이어가 살짝 공중에 떴다가 떨어지는 jitter 현상이 발생함.
// 엘리베이터의 가속을 플레이어의 중력이 바로 따라잡지 못해서 그러는 것 같음...
[RequireComponent(typeof(Rigidbody2D))]
public class Elevator : MonoBehaviour
{
    // 현재 위치에서 얼마나 높이 올라갈지
    [SerializeField] private float movementRange;
    // 이동에 걸리는 시간.
    // 아래로 떨어질 때는 물리 issue 때문에
    // 플레이어가 안정적으로 바닥에 붙어있지 못하므로
    // downwardMovementDuration은 넉넉하게 주는 것이 좋다.
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
        heightLowerBound = rb.position.y;
        heightUpperBound = heightLowerBound + movementRange;
    }

    public void ActivateElevator()
    {
        // 이미 이동 중이라면 요청 무시
        if (isActive)
        {
            return;
        }

        StartElevatorMovementAsync().Forget();
    }

    private async UniTask StartElevatorMovementAsync()
    {
        // 엘리베이터 움직임은 비활성화 상태에서만 시작될 수 있음.
        // 중간에 취소하거나 재시작하는 상황이 일어나면 안됨.
        Assert.IsFalse(isActive);

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
        transform.DOMoveY(destinationHeight, movementDuration).SetEase(movementEase).SetUpdate(UpdateType.Fixed);

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
        Debug.Log("엘리베이터가 곧 움직입니다...");
    }
}
