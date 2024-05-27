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
        // 이미 이동 중이었다면 모션 취소
        if (isMoving)
        {
            CancelMovement();
        }
        // 정지 상태였다면 잠깐 대기한 뒤 이동 시작
        //
        // Note:
        // 플레이어가 엘레베이터를 레버 등으로 작동한 뒤
        // 탑승하러 오기까지 약간의 시간적 여유가 필요함
        else
        {
            PlayerElevatorMoveStartEffect();

            await UniTask.WaitForSeconds(delayBeforeMovement, cancellationToken: movementCancellation.Token);
        }

        isMoving = true;

        // TODO: 더 복잡한 속도 커브가 필요한 경우 DoTween으로 전환할 것
        while (true)
        {
            float heightDiff = destinationHeight - rb.position.y;

            if (Mathf.Abs(heightDiff) > 0.1f)
            {
                rb.velocity = Vector2.up * Mathf.Sign(heightDiff);
                await UniTask.NextFrame(cancellationToken: movementCancellation.Token);
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

    private void CancelMovement()
    {
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
