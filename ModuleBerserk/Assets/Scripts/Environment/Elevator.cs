using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Elevator : MonoBehaviour
{
    [SerializeField] private float movementRange;
    [SerializeField] private float delayBeforeMovement;

    [Serializable]
    private enum State
    {
        Up,
        Down,
    }
    [SerializeField] private State initialState = State.Down;

    private Rigidbody2D rb;
    private float heightUpperBound;
    private float heightLowerBound;

    private CancellationTokenSource movementCancellation = new();
    private bool isMoving = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        CalculateMovementBoundary();
    }

    // 엘리베이터의 y축 이동 범위를 계산함
    private void CalculateMovementBoundary()
    {
        float initialHeight = rb.position.y;
        if (initialState == State.Down)
        {
            heightUpperBound = initialHeight + movementRange;
            heightLowerBound = initialHeight;
        }
        else
        {
            heightUpperBound = initialHeight;
            heightLowerBound = initialHeight - movementRange;
        }
    }

    public void StartMovingUp()
    {
        StartMovement(heightUpperBound).Forget();
    }
    
    public void StartMovingDown()
    {
        StartMovement(heightLowerBound).Forget();
    }

    private async UniTask StartMovement(float destinationHeight)
    {
        // 이번 task에 사용할 토큰.
        // CancellationTokenSource는 취소할 때마다 새로
        // 생성되므로 레퍼런스가 일관적이지 않음
        CancellationToken cancellationToken = movementCancellation.Token;
        
        // 이미 이동 중이었다면 모션 취소
        if (isMoving)
        {
            CancelMovement();
            cancellationToken = movementCancellation.Token; // 새 토큰으로 교체
        }
        // 정지 상태였다면 잠깐 대기한 뒤 이동 시작
        //
        // Note:
        // 플레이어가 엘레베이터를 레버 등으로 작동한 뒤
        // 탑승하러 오기까지 약간의 시간적 여유가 필요함
        else
        {
            PlayerElevatorMoveStartEffect();

            await UniTask.WaitForSeconds(delayBeforeMovement, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }

        isMoving = true;

        // TODO: 더 복잡한 속도 커브가 필요한 경우 DoTween으로 전환할 것
        while (true)
        {
            float heightDiff = destinationHeight - rb.position.y;

            // 아직 목적지와 거리가 있고 목적지를 넘어선 상황도 아니라면 계속 이동
            if (Mathf.Abs(heightDiff) > 0.1f && IsWithinBoundary())
            {
                rb.velocity = Vector2.up * Mathf.Sign(heightDiff);

                await UniTask.NextFrame(cancellationToken: cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
            else
            {
                break;
            }
        }
        
        rb.position = new Vector2(rb.position.x, destinationHeight);
        rb.velocity = Vector2.zero;
        isMoving = false;
    }

    private bool IsWithinBoundary()
    {
        return rb.position.y >= heightLowerBound && rb.position.y <= heightUpperBound;
    }

    private void CancelMovement()
    {
        Debug.Log("이동 취소됨!");
        movementCancellation.Cancel();
        movementCancellation.Dispose();
        movementCancellation = new();
    }

    private void PlayerElevatorMoveStartEffect()
    {
        // TODO: 이제 곧 엘리베이터 움직인다는 효과 재생 ex) "덜그럭" 하는 효과음
        Debug.Log("엘리베이터가 곧 움직입니다...");
    }
}
