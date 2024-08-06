using UnityEngine;

// kinematic body type의 물체는 tweening 등으로 움직여도
// velocity 값이 변하지 않아서 지금 얼마나 빠르게 움직이고 있는지 바로 알 방법이 없음.
// 그래서 이전 프레임과 현재 프레임의 위치 차이를 기반으로 velocity값을 추정해 사용하기로 함.
//
// 이 컴포넌트를 부착하면 FixedUpdate()에서 rigidbody의 velocity 필드를 갱신해준다.
[RequireComponent(typeof(Rigidbody2D))]
public class KinematicRigidbodyVelocityRecorder : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 prevFramePosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        prevFramePosition = rb.position;
    }

    private void FixedUpdate()
    {
        // 슬로우모션에 의한 division-by-zero 방지
        if (!Mathf.Approximately(Time.fixedDeltaTime, 0f))
        {
            rb.velocity = (rb.position - prevFramePosition) / Time.fixedDeltaTime;
            prevFramePosition = rb.position;
        }
    }
}
