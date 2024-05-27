using System;
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
        PlayerElevatorMoveStartEffect();

        await UniTask.WaitForSeconds(delayBeforeMovement);

        // TODO:
        // 1. 더 복잡한 속도 커브가 필요한 경우 DoTween으로 전환할 것
        // 2. 이미 엘리베이터가 이동하는 중인데 또 StartMovement가 호출되는 상황 처리하기
        //    ex) 일회성 트리거 클래스를 새로 만들거나
        //        Elevator가 기존 이동을 취소하고 새로운 방향으로 즉시 전환
        while (true)
        {
            float heightDiff = destinationHeight - rb.position.y;

            if (Mathf.Abs(heightDiff) > 0.1f)
            {
                rb.velocity = Vector2.up * Mathf.Sign(heightDiff);
                await UniTask.NextFrame();
            }
            else
            {
                rb.position = new Vector2(rb.position.x, destinationHeight);
                rb.velocity = Vector2.zero;
                break;
            }
        }
    }

    private void PlayerElevatorMoveStartEffect()
    {
        // TODO: 이제 곧 엘리베이터 움직인다는 효과 재생 ex) "덜그럭" 하는 효과음
        Debug.Log("엘리베이터가 곧 움직입니다...");
    }
}
