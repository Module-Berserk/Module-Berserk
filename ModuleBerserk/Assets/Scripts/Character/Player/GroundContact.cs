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

    private Rigidbody2D rigidbody;
    private BoxCollider2D collider;
    private LayerMask groundLayerMask;
    private float contactDistanceThreshold;

    public GroundContact(Rigidbody2D rigidbody, BoxCollider2D collider, LayerMask groundLayerMask, float contactDistanceThreshold)
    {
        this.rigidbody = rigidbody;
        this.collider = collider;
        this.groundLayerMask = groundLayerMask;
        this.contactDistanceThreshold = contactDistanceThreshold;
    }

    public bool IsSteppingOnOneWayPlatform()
    {
        return IsGrounded && CurrentPlatform.CompareTag("OneWayPlatform");
    }

    public async UniTask IgnoreCurrentPlatformForDurationAsync(float duration)
    {
        var platformCollider = CurrentPlatform.GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(collider, platformCollider);

        await UniTask.WaitForSeconds(duration);

        Physics2D.IgnoreCollision(collider, platformCollider, false);
    }

    public void TestContact()
    {
        CurrentPlatform = FindPlatformBelow();
        IsInContactWithLeftWall = CheckWallContact(Vector2.left);
        IsInContactWithRightWall = CheckWallContact(Vector2.right);
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
        ParallelRaycastResult result = PerformParallelRaycast(ray, offsetFromCenter);

        // 아래에 아무것도 없으면 확실히 바닥과 접촉 중이 아님
        GameObject platform = result.TryGetCollidingObject();
        if (platform == null)
        {
            return null;
        }

        // 땅에 가만히 서있거나 (상대 속도 = 0) 움직이는 엘리베이터에 서있는 경우를
        // 점프해서 one way platform을 뚫고 올라는 경우(상대 속도 != 0)를 구분
        var isElevator = platform.GetComponent<Elevator>() != null;
        var platformRigidbody = platform.GetComponent<Rigidbody2D>();
        if (!isElevator && Mathf.Abs(platformRigidbody.velocity.y - rigidbody.velocity.y) > 0.01f)
        {
            return null;
        }

        // 만약 아래에 플랫폼이 있다면 그 중에서도 끝자락에 위치한 상태인지 테스트
        IsRightFootGrounded = result.Hit1;
        IsLeftFootGrounded = result.Hit2;
        return platform;
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
        ParallelRaycastResult result = PerformParallelRaycast(ray, offsetFromCenter);

        return result.IsBothRayHit();
    }

    // 벽 또는 바닥과 접촉 중인지 확인하기 위해
    // 콜라이더의 위/아래 또는 양옆에서 평행하게 raycast를 수행함.
    // 그 결과를 저장하고 해석을 돕는 메소드를 제공하는 클래스.
    private struct ParallelRaycastResult
    {
        public RaycastHit2D Hit1;
        public RaycastHit2D Hit2;

        // 두 raycast 결과 중에 충돌한 물체가 있다면 반환하고,
        // 둘 다 실패한 경우에는 null을 반환함.
        // 지면과 접촉 중인지 테스트할 때 사용됨.
        public GameObject TryGetCollidingObject()
        {
            if (Hit1)
            {
                return Hit1.collider.gameObject;
            }
            else if (Hit2)
            {
                return Hit2.collider.gameObject;
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
    private ParallelRaycastResult PerformParallelRaycast(Vector2 ray, Vector2 offsetFromCenter)
    {
        Vector2 center = GetColliderCenter();

        // ray 파라미터에는 중심에서 콜라이더의 경계까지 도달하는 벡터가 주어짐.
        // 하지만 정확히 경계면까지 raycast를 하는 경우 벽에 닿아있어도 가끔 검출에 실패할 수 있음.
        // 이런 일관적이지 않은 결과를 방지하기 위해 콜라이더의 경계로부터 거리가
        // contactDistanceThreshold 이하인 경우는 접촉 중인 것으로 취급함.
        float raycastDistance = ray.magnitude + contactDistanceThreshold;

        return new ParallelRaycastResult
        {
            Hit1 = Physics2D.Raycast(center + offsetFromCenter, ray, raycastDistance, groundLayerMask),
            Hit2 = Physics2D.Raycast(center - offsetFromCenter, ray, raycastDistance, groundLayerMask)
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
