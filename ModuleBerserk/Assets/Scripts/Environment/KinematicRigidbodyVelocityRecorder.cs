using UnityEngine;

// kinematic body type의 물체는 tweening으로 움직여도
// velocity 값이 변하지 않아서 지금 얼마나 빠르게 움직이고 있는지 바로 알 방법이 없음.
// 그래서 이전 프레임과 현재 프레임의 위치 차이를 기반으로 velocity값을 추정해 사용하기로 함.
//
// 이 컴포넌트를 부착하면 FixedUpdate()에서 rigidbody의 velocity 필드를 갱신해준다.
//
// 반드시 tweening이 시작될 때 컴포넌트를 활성화하고
// tweening이 끝난 즉시 비활성화해줘야 velocity값이 오염되지 않음!
[RequireComponent(typeof(Rigidbody2D))]
public class KinematicRigidbodyVelocityRecorder : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 prevFramePosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // kinematic rigidbody는 velocity로 못 움직인다고 알고 있었는데
    // 가끔 보니까 이 컴포넌트에 의해 설정된 velocity가 물체를 마구 움직여버리는 경우가 있었음.
    // ex) Ease.InFlash를 사용한 경우 마지막 속도가 0보다 크게 남아있음
    //
    // tweening으로 움직이는 동안만 사용하도록 컴포넌트 자체를 동적으로 활성화/비활성화함!
    //
    // tweening이 시작될 때에는 기준점으로 삼을 지점을 기록해야 하고
    // tweening이 끝나면 잔여 velocity를 없애줘야 함
    private void OnEnable()
    {
        prevFramePosition = rb.position;
    }

    private void OnDisable()
    {
        rb.velocity = Vector2.zero;
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
