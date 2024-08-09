using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 캐릭터가 바닥을 밟고 있는지, 또는 양 옆의 벽에 닿아있는지 체크하는 클래스
public class GroundContact
{
    public GameObject CurrentPlatform {get; private set;}
    public bool IsGrounded {get => CurrentPlatform != null;}
    public bool IsLeftFootGrounded {get; private set;}
    public bool IsRightFootGrounded {get; private set;}
    public bool IsInContactWithLeftWall {get; private set;}
    public bool IsInContactWithRightWall {get; private set;}
    public bool IsSteppingOnMovingPlatform
    {
        get => CurrentPlatform != null && IsObjectMovingPlatform(CurrentPlatform);
    }

    private bool IsObjectMovingPlatform(GameObject gameObject)
    {
        return gameObject.CompareTag("MovingPlatform");
    }

    // IsGrounded가 true인 경우에만 유효한 세부 정보.
    // Normal은 지면과 수직인 윗 방향을, Tangent는 지면과 평행한 오른쪽 방향을 나타냄.
    public Vector2 GroundNormal {get; private set;}
    public Vector2 GroundTangent
    {
        get
        {
            // Note: 유니티 좌표축이 수학 시간에 배우던거랑 z가 반대라서 오른손 법칙이 안먹힘
            return Vector3.Cross(Vector3.back, GroundNormal);
        }
    }

    private Rigidbody2D rb;
    private BoxCollider2D collider;
    private LayerMask groundLayerMask;
    private float contactDistanceThreshold;
    private float slopeContactDistanceThreshold;

    // 플레이어가 경사로를 타고 올라가다가 점프하는 상황을 잘 처리하기 위한 타이머.
    // 자세한 설명은 PreventTestForDuration() 함수의 주석에 있음.
    private float forceTestFailureDuration = 0f;

    public GroundContact(Rigidbody2D rigidbody, BoxCollider2D collider, LayerMask groundLayerMask, float contactDistanceThreshold = 0.02f, float expectedSlopeAngleInDegrees = 45f)
    {
        rb = rigidbody;
        this.collider = collider;
        this.groundLayerMask = groundLayerMask;
        this.contactDistanceThreshold = contactDistanceThreshold;

        // 경사로에 서있는 경우 한쪽 발이 닿아있어도 다른 발은 지면에서 멀리 떨어져있으니
        // IsLeftFootGrounded 같은 속성을 정확히 체크하려면 살짝 더 먼 거리까지 raycast해야 함
        slopeContactDistanceThreshold = contactDistanceThreshold + collider.size.x * Mathf.Tan(expectedSlopeAngleInDegrees * Mathf.Deg2Rad);
    }

    public async UniTask IgnoreCurrentPlatformForDurationAsync(float duration)
    {
        var platformCollider = CurrentPlatform.GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(collider, platformCollider);

        await UniTask.WaitForSeconds(duration);

        Physics2D.IgnoreCollision(collider, platformCollider, false);
    }

    // FixedUpdate에서 호출되어야 하는 함수.
    // 현재 아래에 플랫폼이 있는지, normal 벡터는 무엇인지 등을 찾아낸다.
    public void TestContact()
    {
        // 경사로를 타고 올라가다가 점프했을 때 바로 착지해버리는 것을 막기 위한 처리.
        // 자세한 설명은 PreventTestForDuration() 함수 주석에 있음.
        forceTestFailureDuration -= Time.fixedDeltaTime;
        if (forceTestFailureDuration > 0f)
        {
            CurrentPlatform = null;
        }
        else
        {
            CurrentPlatform = FindPlatformBelow();
        }

        // 벽과의 충돌은 점프 여부와 무관하게 항상 정상적으로 테스트함
        IsInContactWithLeftWall = CheckWallContact(Vector2.left);
        IsInContactWithRightWall = CheckWallContact(Vector2.right);
    }

    // 경사로를 타고 올라가다가 점프하면 바로 아래에 있는 경사로에
    // 즉시 착지한 것으로 판단하는 상황을 막기 위해 플레이어가 점프할 때마다
    // 아주 짧은 시간동안 IsGrounded가 true로 바뀌는 것을 방지함.
    // 벽과의 충돌 검출은 영향받지 않는다.
    public void PreventTestForDuration(float duration)
    {
        forceTestFailureDuration = duration;
    }

    // 콜라이더의 양 옆에서 아래로 raycast해서 바닥과 접촉 중인지 확인하고
    // 만약 접촉 중이라면 바닥의 GameObject를, 아니라면 null을 반환.
    //
    // note:
    // 중심에서 raycast를 하면 플랫폼의 끝 걸쳐있을 때 false가 나와서
    // 캐릭터는 착지했는데 낙하 중인 것으로 판정될 수 있으므로
    // 이를 방지하기 위해 양쪽을 모두 확인해줘야 함.
    private GameObject FindPlatformBelow()
    {
        // 이전 상태 초기화
        IsLeftFootGrounded = false;
        IsRightFootGrounded = false;

        // 0.99f 곱하는 이유: 정확히 콜라이더의 양 끝에서 시작하면 벽을 바닥으로 착각할 수 있음
        var offsetFromCenter = Vector2.right * collider.size.x / 2f * 0.99f;
        var ray = Vector2.down * collider.size.y / 2f;
        ParallelRaycastResult result = PerformParallelRaycast(ray, offsetFromCenter, slopeContactDistanceThreshold);

        // 만약 양쪽 발 아래 거리가 판정 임계점 이상으로 나왔다면 공중에 있는 것으로 판단
        float rightFootDistance = result.Hit1Distance();
        float leftFootDistance = result.Hit2Distance();
        if (Mathf.Min(rightFootDistance, leftFootDistance) > contactDistanceThreshold)
        {
            return null;
        }

        // 여기까지 왔다면 둘 중 한쪽 발은 확실히 지면 위에 있고,
        // 다른 하나는 경사로인 경우 살짝 떠있을 가능성이 있음.
        Rigidbody2D platform = result.TryGetCollidingObject();

        // 아래에 뭔가 있으니 일단 지면의 normal 벡터를 기록.
        // 왼쪽과 오른쪽 모두 충돌했다면 둘 중 지면과 거리가 가까운 쪽을 참고함.
        GroundNormal = (rightFootDistance < leftFootDistance) ? result.Hit1.normal : result.Hit2.normal;

        // 땅에 가만히 서있는 경우를 (상대 속도 = 0) 점프해서
        // one way platform을 뚫고 올라는 경우(수직 방향 상대 속도 != 0)와 구분.
        // 각도가 다른 경사로 사이를 넘어갈 때는 relativeNormalVelocity가 크게 나올 수도 있으니
        // 이전 프레임에 IsGrounded인 경우에는 고려하지 않는다.
        //
        // Note:
        // 경사로를 따라 움직일 수도 있으니 속도의 y축 성분이 아니라
        // 지면과의 normal 벡터 방향 성분을 기준으로 삼아야 함!!!
        var relativeVelocity = rb.velocity - platform.velocity;
        var relativeNormalVelocity = Vector2.Dot(GroundNormal, relativeVelocity);
        if (!IsGrounded && relativeNormalVelocity > 0.1f)
        {
            // Debug.Log($"수직 속도가 0.1 이상이어서 착지로 판단하지 않음!\nvelocity: {rb.velocity}, other velocity: {platform.velocity}", platform);
            return null;
        }

        // 만약 아래에 플랫폼이 있다면 그 중에서도 끝자락에 위치한 상태인지 테스트
        IsRightFootGrounded = result.Hit1;
        IsLeftFootGrounded = result.Hit2;

        return platform.gameObject;
    }

    // Vector2.left 또는 Vector2.right가 주어졌을 때 해당 방향으로
    // 콜라이더의 위와 아래에서 옆으로 raycast해서 벽과 접촉 중인지 확인함.
    //
    // note:
    // 중심에서 raycast를 하면 벽의 위 또는 아래 경계 근처에서 true가 나와서
    // 손이나 발이 붕 뜬 상태로 벽에 매달릴 수 있으므로
    // 이를 방지하기 위해 양쪽을 모두 확인해줘야 함.
    private bool CheckWallContact(Vector2 direction)
    {
        var offsetFromCenter = Vector2.up * collider.size.y / 2f;
        var ray = direction * collider.size.x / 2f;
        ParallelRaycastResult result = PerformParallelRaycast(ray, offsetFromCenter, contactDistanceThreshold);

        return result.IsBothRayHit();
    }

    // 벽 또는 바닥과 접촉 중인지 확인하기 위해
    // 콜라이더의 위/아래 또는 양옆에서 평행하게 raycast를 수행함.
    // 그 결과를 저장하고 해석을 돕는 메소드를 제공하는 클래스.
    private struct ParallelRaycastResult
    {
        public RaycastHit2D Hit1;
        public RaycastHit2D Hit2;

        public float Hit1Distance()
        {
            return Hit1 ? Hit1.distance : Mathf.Infinity;
        }

        public float Hit2Distance()
        {
            return Hit2 ? Hit2.distance : Mathf.Infinity;
        }

        // 두 raycast 결과 중에 충돌한 물체가 있다면 반환하고,
        // 둘 다 실패한 경우에는 null을 반환함.
        // 지면과 접촉 중인지 테스트할 때 사용됨.
        public Rigidbody2D TryGetCollidingObject()
        {
            if (Hit1)
            {
                return Hit1.collider.attachedRigidbody;
            }
            else if (Hit2)
            {
                return Hit2.collider.attachedRigidbody;
            }
            else
            {
                return null;
            }
        }

        // 두 raycast가 모두 성공했는지 반환.
        // 벽과 접촉 중인지 테스트할 때 사용됨.
        public bool IsBothRayHit()
        {
            return Hit1 && Hit2;
        }
    }

    // ray: 중심에서 접촉을 확인할 방향의 콜라이더 경계까지의 벡터
    // offsetFromCenter: 콜라이더 중심으로부터 ray의 시작점까지의 벡터 (다른 한 ray는 반대 방향에서 시작)
    private ParallelRaycastResult PerformParallelRaycast(Vector2 ray, Vector2 offsetFromCenter, float contactDistanceThreshold)
    {
        Vector2 center = GetColliderCenter();

        // 경계면에서 지형까지의 거리가 contactDistanceThreshold 이하인 경우는 접촉 중인 것으로 취급함.
        return new ParallelRaycastResult
        {
            Hit1 = Physics2D.Raycast(center + ray + offsetFromCenter, ray, contactDistanceThreshold, groundLayerMask),
            Hit2 = Physics2D.Raycast(center + ray - offsetFromCenter, ray, contactDistanceThreshold, groundLayerMask)
        };
    }

    // collider.transform.position은 콜라이더의 중심이 아니라 gameobject의
    // 위치를 반환하기 때문에 콜라이더의 offset을 고려한 위치를 계산해야 함.
    private Vector2 GetColliderCenter()
    {
        Vector2 center = collider.transform.position;
        center += collider.offset;

        return center;
    }
}
