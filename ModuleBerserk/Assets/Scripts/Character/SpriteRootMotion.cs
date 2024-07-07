using UnityEngine;

// 공격 애니메이션 등 이동이 복잡한 경우 스프라이트의 pivot을
// 기준으로 rigidbody velocity를 설정해주는 스크립트.
public class SpriteRootMotion : MonoBehaviour
{
    // 스프라이트의 픽셀 단위가 유니티의 크기 단위와 달라서 약간의 보정이 필요함
    [SerializeField] private float motionScale = 1.2f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    // 새로운 애니메이션이 시작된 경우 이전 애니메이션과 pivot 기준점이 다를 수 있음.
    // 이 상황에서 이전 애니메이션의 pivot과 새 애니메이션의 pivot의 차이를 root motion으로 줘버리면
    // 제자리에 서있어야 하는데도 캐릭터가 이동해버리는 문제가 생김!
    // 이를 방지하기 위해 애니메이션 전환이 일어나는 몇 프레임 동안은 루트 모션을 비활성화함.
    private int numFramesDisableRootMotion = 0;
    // 이전 프레임의 pivot 좌표를 기억해 현재 pivot 좌표와의 차이를 velocity로 사용.
    private float prevSpritePivotX;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    // 원본 공격 애니메이션들을 보면 플레이어가 중심 위치에
    // 가만히 있지 않고 pivot을 기준으로 조금씩 이동함.
    // 이걸 그냥 쓰면 플레이어 오브젝트는 가만히 있는데
    // 스프라이트만 이동하는 것처럼 보이므로 굉장히 이상해짐...
    //
    // 지금 사용하는 방식:
    // 1. 공격 애니메이션의 프레임 별 pivot이 항상 플레이어의 중심에 오도록 수정
    //    => 이제 애니메이션 재생해도 플레이어는 제자리에 있는 것처럼 보임
    // 2. 스프라이트 상의 이동을 실제 플레이어 오브젝트의 이동으로 변환하기 위해 pivot 변화량을 계산
    //    => pivot 변화량에 비례해 velocity를 부여해서 원본 에셋의 이동하는 느낌을 물리적으로 재현
    public void ApplyVelocity(bool isFacingLeft)
    {
        float currSpritePivotX = spriteRenderer.sprite.pivot.x;

        // 모션이 방금 바뀐 경우에는 기준으로 삼아야 할 pivot 값을 아직 모르니까
        // 루트 모션 적용은 스킵하고 prevSpritePivotX 값만 갱신함.
        // 자세한 설명은 TriggerNextAttack()에서 이 변수를 수정하는 부분 참고할 것.
        if (numFramesDisableRootMotion > 0)
        {
            numFramesDisableRootMotion--;
        }
        else
        {
            // 스프라이트의 pivot이 커졌다는 것은 플레이어의 중심 위치가
            // 오른쪽으로 이동했다는 뜻이므로 오른쪽 방향으로 속도를 주면 됨.
            float rootMotion = currSpritePivotX - prevSpritePivotX;

            // 스프라이트는 항상 오른쪽만 바라보니까 루트 모션도 항상 오른쪽으로만 나옴.
            // 실제 바라보는 방향으로 이동할 수 있도록 왼쪽 또는 오른쪽 벡터를 선택함.
            // 마지막에 곱하는 상수는 원본 애니메이션과 비슷한 이동 거리가 나오도록 실험적으로 구한 수치.
            float verticalVelocity = rb.velocity.y;
            float horizontalVelocity = (isFacingLeft ? -1f : 1f) * rootMotion * motionScale;
            rb.velocity =  new Vector2(horizontalVelocity, verticalVelocity);
        }

        prevSpritePivotX = currSpritePivotX;
    }

    // 애니메이션의 pivot 변화로 루트모션을
    // 적용하기 때문에 새로운 애니메이션을 시작할 때마다 기준점을 잡아줘야 함.
    //
    // 2프레임을 대기해야 하는 이유는 다음과 같음:
    // 1. 유니티는 애니메이션 이벤트를 FixedUpdate보다 먼저 처리함
    // 2. 연속 공격의 경우 이 함수가 OnAttackMotionEnd()에 의해 호출됨
    // 3. 루트 모션 처리는 FixedUpdate에서 일어남
    // 4. 그러므로 루트모션 비활성화는 "이번 프레임을 포함한" 2프레임동안 일어나게 됨
    //
    //   [이전 애니메이션] n번 프레임 <-- numFramesDisableRootMotion 2에서 1로 감소
    //   [다음 애니메이션] 1번 프레임 <-- numFramesDisableRootMotion 1에서 0으로 감소, 새로운 pivot 기준점 기록!
    //   [다음 애니메이션] 2번 프레임 <-- 새 애니메이션의 1번 프레임 기준으로 root motion 적용 가능
    public void HandleAnimationChange()
    {
        numFramesDisableRootMotion = 2;
    }
}
