using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Trigger와 조건부 상호작용을 테스트하기 위해 만든 클래스.
// 문이 열리고 닫히는 것을 위/아래로 움직이는 것으로 표현한다.
// 위치가 바뀌므로 rigidbody 타입이 kinematic이어야 함!
public class TestDoor : MonoBehaviour
{
    // 문이 열리고 닫힐 때 위치가 얼마나 바뀌는지
    [SerializeField] private float doorMovementRange = 2f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Open()
    {
        rb.position += Vector2.up * doorMovementRange;
    }

    public void Close()
    {
        rb.position += Vector2.down * doorMovementRange;
    }
}
