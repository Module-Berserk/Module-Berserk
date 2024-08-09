using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

// 경사로 이동, 엘리베이터 위에서 속도 동기화 등
// 각종 골치아픈 이동 로직을 담당하는 컴포넌트.
// 주인공과 적 모두 이 컴포넌트를 통해 이동한다.
//
// 크게 보면 두 가지 방식으로 작동한다.
//
// 1. 정적인 플랫폼 위에 있는 상황
//    - dynamic rigidbody + velocity 기반 이동
//    - 자동으로 제공되는 지형 충돌을 최대한 이용함
//    - 평소에는 공중에서나 평지에서나 일정한 속도로 이동할 수 있도록
//      마찰력을 0으로 만들지만, 정지한 상황에서는 경사로에서도
//      멈춰있도록 아아아아주 높은 마찰력을 사용하도록 한다,
//
// 2. 동적인 플랫폼 위에 있는 상황
//    - kinematic rigidbody + localPosition 기반 이동
//    - 일시적으로 캐릭터가 플랫폼의 자식 오브젝트로 들어가게 된다
//    - kinematic rigidbody는 hierarchy상의 부모 오브젝트가
//      움직이면 자신도 같이 움직이기 때문에 플랫폼의 이동 속도와
//      캐릭터의 이동 속도가 자동으로 동기화됨.
//      dynamic rigidbody는 이런 특성이 없어서 자식으로 넣어줘도 차이 x
//    - 대신 velocity나 force 기반의 움직임이 전혀 먹히지 않으므로
//      velocity 값을 보고 localPosition을 직접 변경해줘야 함
public class PlatformerMovement : MonoBehaviour
{
    [Header("Walk / Run")]
    [SerializeField] private float turnAcceleration = 150f;
    [SerializeField] private float moveAcceleration = 100f;
    [SerializeField] private float moveDecceleration = 150f;
    // 서있을 때는 마찰력을 높게 줘서 경사로에서도 미끄러지지 않도록 만듦
    [SerializeField] private PhysicsMaterial2D zeroFrictionMat;
    [SerializeField] private PhysicsMaterial2D maxFrictionMat;

    
    [Header("Jump / Fall")]
    [SerializeField] private float jumpVelocity = 12f;
    [SerializeField] private Vector2 wallJumpVelocity = new(10f, 10f);
    // 땅에서 떨어져도 점프를 허용하는 time window
    [SerializeField] private float coyoteTime = 0.15f;
    // 공중에 있지만 위로 이동하는 중이라면 DefaultGravityScale을 사용하고
    // 아래로 이동하는 중이라면 GravityScaleWhenFalling을 사용해
    // 더 빨리 추락해서 공중에 붕 뜨는 이상한 느낌을 줄임.
    [SerializeField] private float defaultGravityScale = 3f;
    [SerializeField] private float gravityScaleWhileFalling = 6f;
    // 아주 높은 곳에서 떨어질 때 부담스러울 정도로 아래로 가속하는 상황 방지
    [SerializeField] private float maxFallSpeed = 15f;
    // 공중 조작이 지상 조작보다 둔하게 만드는 파라미터 (0: 조작 불가, 1: 변화 없음)
    [SerializeField, Range(0f, 1f)] private float defaultAirControl = 0.5f;
    // wall jump 이후 벽으로 돌아오는데에 걸리는 시간을 조정하는 파라미터 (airControl을 잠시 이 값으로 대체함)
    [SerializeField, Range(0f, 1f)] private float airControlWhileWallJumping = 0.2f;
    // wall jump 이후 defaultAirControl 대신 airControlWhileWallJumping을 적용할 기간
    [SerializeField, Range(0f, 1f)] private float wallJumpAirControlPenaltyDuration = 0.3f;

    
    [Header("Evasion")]
    [SerializeField] private float evasionDuration = 0.2f; // 회피 모션의 재생 시간과 일치해야 자연스러움!
    [SerializeField] private float evasionDistance = 4f;
    [SerializeField] private Ease evasionEase = Ease.OutCubic;


    [Header("Ground Contact")]
    // 땅으로 취급할 layer를 모두 에디터에서 지정해줘야 함!
    [SerializeField] private LayerMask groundLayerMask;
    // 콜라이더로부터 이 거리보다 가까우면 접촉 중이라고 취급.
    [SerializeField] private float contactDistanceThreshold = 0.05f;
    [SerializeField] private float expectedSlopeAngleInDegrees = 45f;


    [Header("Debug")]
    [SerializeField] private bool logFrictionChange = false;


    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    // 지면 접촉 테스트 관리자
    private GroundContact groundContact;
    // 땅에서 떨어진 시점부터 Time.deltaTime을 누적하는 카운터로,
    // 이 값이 CoyoteTime보다 낮을 경우 isGrounded가 false여도 점프 가능.
    private float coyoteTimeCounter = 0f;
    // 땅에 닿기 전에 점프한 횟수.
    // 더블 점프 처리에 사용됨.
    private int jumpCount = 0;
    // 벽에 붙어있다가 떨어지는 순간의 coyote time과
    // 그냥 달리다가 떨어지는 순간의 coyote time을 구분하기 위한 상태 변수.
    // 점프를 일반 점프로 할지 wall jump로 할지 결정한다.
    private bool shouldWallJump = false;
    // 현재 어느 쪽을 바라보고 있는지 기록.
    // 스프라이트 반전과 카메라 추적 위치 조정에 사용됨.
    private bool isStickingToRightWall;
    // defaultAirControl과 airControlWhileWallJumping 중 실제로 적용될 수치
    private float airControl;

    // 착지 시점을 판단하기 위해 이전 프레임에 플랫폼을 밟고 있었는지 기록함
    private GameObject prevFramePlatform = null;

    public bool IsGrounded { get => groundContact.IsGrounded; }
    public bool IsSteppingOnOneWayPlatform { get => groundContact.IsGrounded && groundContact.CurrentPlatform.GetComponent<PlatformEffector2D>() != null; }
    public bool IsStickingToMovingPlatform { get => transform.parent != null; }

    public UnityEvent OnLand;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();

        groundContact = new(rb, boxCollider, groundLayerMask, contactDistanceThreshold, expectedSlopeAngleInDegrees);
        airControl = defaultAirControl;
    }

    public void HandleGroundContact()
    {
        groundContact.TestContact();
        if (IsGrounded)
        {
            // 착지 이벤트
            if (prevFramePlatform == null)
            {
                OnLand.Invoke();

                ResetJumpStates();
            }

            // 엘리베이터 위에 서있는 동안은 움직임 동기화
            if (ShouldStickToMovingPlatform())
            {
                StartStickingToMovingPlatform();
            }
            else if (IsStickingToMovingPlatform && !groundContact.IsSteppingOnMovingPlatform)
            {
                StopStickingToMovingPlatform();
            }
        }
        else
        {
            HandleCoyoteTime();
            ApplyDynamicGravity();
            ClampFallingVelocity();

            // 추락하면 엘리베이터와의 이동 동기화 중지
            if (IsStickingToMovingPlatform)
            {
                StopStickingToMovingPlatform();
            }
        }

        prevFramePlatform = groundContact.CurrentPlatform;
    }

    private void ResetJumpStates()
    {
        jumpCount = 0;
        shouldWallJump = false;
        coyoteTimeCounter = 0f;
        rb.gravityScale = defaultGravityScale;
    }

    public void FallThroughPlatform()
    {
        StopStickingToMovingPlatform();
        groundContact.PreventTestForDuration(0.2f);
        groundContact.IgnoreCurrentPlatformForDurationAsync(0.5f).Forget();
    }

    private bool ShouldStickToMovingPlatform()
    {
        return !IsStickingToMovingPlatform && groundContact.IsSteppingOnMovingPlatform;
    }

    // 엘리베이터 위로 올라가는 순간 한 번 호출되는 함수로
    // 엘리베이터의 이동과 캐릭터의 이동을 동기화하도록 설정함.
    //
    // dynamic rigidbody는 완전히 독립적인 존재라서
    // 오브젝트의 부모-자식 관계가 이동에 영향을 주지 않음.
    //
    // 이는 캐릭터가 엘리베이터 위에서 서있거나 이동하는 것을 이상하게 만듦.
    // ex) 아래로 움직이는 엘리베이터의 이동 속도를 중력이 바로 따라잡지 못해 낙하와 착지를 반복
    //
    // 이를 해결하기 위해 엘리베이터처럼 이동하는 플랫폼 위에 있는 기간에 한해
    // 아예 rigidbody를 kinematic으로 만들어버리고
    // velocity에 의한 이동을 localPosition의 이동으로 치환함.
    private void StartStickingToMovingPlatform()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.transform.SetParent(groundContact.CurrentPlatform.transform);
    }

    // 엘리베이터 위에서 벗어났을 때 움직임 동기화 설정을 원래대로 돌려놓음.
    // 이런 작업이 필요한 이유는 StartStickingToMovingPlatform()의 주석 참고.
    private void StopStickingToMovingPlatform()
    {
        rb.transform.SetParent(null);
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void HandleCoyoteTime()
    {
        coyoteTimeCounter += Time.fixedDeltaTime;

        // 방금 전까지 벽에 매달려있었더라도 coyote time을 넘어서면
        // 일반적인 더블 점프로 취급 (점프해도 위로만 상승)
        if (coyoteTimeCounter > coyoteTime)
        {
            shouldWallJump = false;
        }
    }
    
    // 최대 추락 속도 제한
    private void ClampFallingVelocity()
    {

        if (rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }
    }

    private void ApplyDynamicGravity()
    {
        // 현재 추락하는 중이라면 더 강한 중력을 사용해서 붕 뜨는 느낌을 줄임.
        bool isFalling = rb.velocity.y < -0.01f;
        if (isFalling)
        {
            UseGravityScaleWhileFalling();
        }
        else
        {
            UseDefaultGravityScale();
        }
    }

    public void UseGravityScaleWhileFalling()
    {
        rb.gravityScale = gravityScaleWhileFalling;
    }

    public void UseDefaultGravityScale()
    {
        rb.gravityScale = defaultGravityScale;
    }

    // 원하는 가로축 목표 속도를 넣어주면 부드럽게 가속/감속하도록 만듦.
    // Case 1) 지면에 서있는 경우 => 지면과 평행한 방향으로 이동
    // Case 2) 공중에 있는 경우 => 수직 속도는 유지하고 수평 속도만 조정
    //
    // Note:
    // 경사로에 있는 경우 GroundTangent가 수평이 아니다!
    // 이 상황에서 수평으로 움직이면 낙하-착지를 반복하게 될 수 있으므로
    // 지면과 평행하게 움직여주는게 중요함.
    public void UpdateMoveVelocity(float desiredSpeed, bool skipAcceleration = false)
    {
        // 목표 속도에 부드럽게 도달하도록 가속/감속한 속도를 계산.
        rb.velocity = CalculateUpdatedVelocity(desiredSpeed, skipAcceleration);

        if (rb.bodyType == RigidbodyType2D.Kinematic)
        {
            MoveAsKinematicBody();
        }
    }

    // 엘리베이터처럼 동적인 플랫폼 위에 서있는 경우 플랫폼과
    // 속도를 동기화하기 위해 kinematic rigidbody로 동작하게 되는데,
    // 이러면 평소에 기본으로 처리되던 벽과의 충돌이나 velocity를 통한 이동이 먹히지 않음!
    // ex) kinematic 상태에서는 벽을 다 뚫고 지나감
    //
    // 그래서 수동으로 내가 탑승중인 플랫폼 이외의 벽과 충돌했는지 확인하고
    // 만약 그렇다면 뚫고 가지 못하도록 충돌이 일어난 지점으로 옮겨줘야 함.
    // velocity에 따른 이동은 충돌이 없는 경우에만 처리됨.
    private void MoveAsKinematicBody()
    {
        Vector3 movement = rb.velocity * Time.fixedDeltaTime;

        // 충돌 검사 도중에 아무 문제도 없는 경우에만 계획대로 움직임
        bool collisionExists = ResolveKinematicCollision(movement);
        if (!collisionExists)
        {
            transform.localPosition += movement;
        }
    }

    // ResolveKinematicCollision() 함수에서 BoxCollider2D.Cast()의
    // 결과를 저장하는 배열로, documentation에 따르면 매번 새로운 배열을 할당하지
    // 않고 미리 만들어둔 버퍼를 재사용하는 방식이라서 가비지 콜렉션 부담을 줄여준다고 함...
    private RaycastHit2D[] hits = new RaycastHit2D[5];

    // kinematic rigidbody 상태에서도 마치 dynamic rigidbody인 것처럼
    // 벽 등의 물체와 충돌하는 상황을 해소해주는 함수.
    // 위치 조정이 필요한 충돌이 있었다면 true를, 아무 문제도 없었다면 false를 반환한다.
    //
    // Rigidbody2D.Cast()는 잡몹에 달린 주인공 감지 범위로 쓰는 콜라이더까지
    // 다 포함해서 충돌을 검출해버리므로 반드시 본체의 box collider로만 cast해야 함!
    private bool ResolveKinematicCollision(Vector3 movement)
    {
        int numHits = boxCollider.Cast(movement, hits, distance: movement.magnitude);
        for (int i = 0; i < numHits; ++i)
        {
            if (NeedCollisionResolution(hits[i], movement))
            {
                MoveToContactPoint(hits[i].normal, hits[i].collider);

                return true;
            }
        }

        return false;
    }

    // kinematic rigidbody 상태에서 충돌을 해소할 필요가 없는 경우:
    // 1. 지금 내가 타고있는 이동 플랫폼과의 충돌
    // 2. 무기 히트박스 등 나의 자식 오브젝트
    //    * 히트박스는 여러개의 콜라이더를 보유하기 위해
    //      자신만의 kinematic rigidbody를 가지고 있음
    // 3. 상대가 트리거로 설정된 콜라이더
    private bool NeedCollisionResolution(RaycastHit2D hit, Vector2 movement)
    {
        bool isCurrentPlatform = hit.transform.gameObject == groundContact.CurrentPlatform;
        bool isChildObject = hit.transform.parent == gameObject.transform;
        bool isTrigger = hit.collider.isTrigger;

        return !isCurrentPlatform && !isChildObject && !isTrigger;
    }

    // kinematic rigidbody 상태에서 다른 물체와 충돌했을 때 penetration을 없애주는 함수.
    //
    // 작동 원리:
    // - 내 콜라이더가 normal 방향으로 살짝 이동했다고 치고
    //   해당 위치에서 normal과 반대 방향으로 raycast
    //   * 살짝 떨어진 곳에서 내가 상대와 충돌하러 날아오는 상황을 재현
    // - raycast 자체에는 내가 밟고있는 플랫폼 등 잡다한 결과가 다 들어있으므로
    //   결과 리스트를 순회하며 내가 찾는 상대 콜라이더와의 RaycastHit2D를 탐색
    // - 여기에 들어있는 centroid 값이 바로 내가 멈췄어야 할 지점
    private void MoveToContactPoint(Vector2 collisionNormal, Collider2D other)
    {
        const float maxPenetrationDepth = 0.5f;
        Vector2 origin = rb.position + boxCollider.offset + collisionNormal * maxPenetrationDepth;
        Vector2 direction = -collisionNormal;
        RaycastHit2D[] results = Physics2D.BoxCastAll(origin, boxCollider.size, 0f, direction, maxPenetrationDepth);

        foreach (var hit in results)
        {
            if (hit.collider == other)
            {
                // centroid는 콜라이더의 위치라서 본체 transform의 위치는
                // 역으로 콜라이더의 offset을 빼줘야 구할 수 있음
                transform.position = hit.centroid - boxCollider.offset;
                break;
            }
        }
    }


    private Vector2 CalculateUpdatedVelocity(float desiredSpeed, bool skipAcceleration)
    {
        // 가속해야 할 방향으로의 속도 성분 (공중인 경우 중력 고려 x)
        Vector2 moveDirection = IsGrounded ? groundContact.GroundTangent : Vector2.right;
        float currentSpeed = Vector2.Dot(moveDirection, rb.velocity);

        // 방향 전환 여부에 따라 다른 가속도 사용
        float acceleration = ChooseAcceleration(desiredSpeed);

        // 공중이라면 AirControl 수치(0.0 ~ 1.0)에 비례해 가속도 감소
        if (!IsGrounded)
        {
            acceleration *= airControl;
        }

        // 원하는 속도에 부드럽게 도달하도록 보간.
        // 공중에 있는 경우는 수직 속도 변경 x
        float updatedSpeed = skipAcceleration ? desiredSpeed : Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * Time.deltaTime);

        Vector2 updatedVelocity = moveDirection * updatedSpeed;
        if (!IsGrounded)
        {
            updatedVelocity.y = rb.velocity.y;
        }

        return updatedVelocity;
    }


    // 가만히 서있는 상황에서는 아주 높은 마찰력을 적용해
    // 경사로나 이동 플랫폼 등에서 미끄러지지 않도록 함
    public void UpdateFriction(float desiredSpeed)
    {
        if (Mathf.Approximately(desiredSpeed, 0f))
        {
            ApplyHighFriction();
        }
        else
        {
            ApplyZeroFriction();
        }
    }

    public void ApplyHighFriction()
    {
        rb.sharedMaterial = maxFrictionMat;

        if (logFrictionChange)
        {
            Debug.Log("{[PlatformerMovement] high friction}");
        }
    }

    public void ApplyZeroFriction()
    {
        rb.sharedMaterial = zeroFrictionMat;
        
        if (logFrictionChange)
        {
            Debug.Log("{[PlatformerMovement] zero friction}");
        }
    }

    private float ChooseAcceleration(float desiredSpeed)
    {
        // Case 1) 이동을 멈추는 경우
        if (Mathf.Approximately(desiredSpeed, 0f))
        {
            return moveDecceleration;
        }

        // Case 2) 반대 방향으로 이동하려는 경우
        bool isTurningDirection = rb.velocity.x * desiredSpeed < 0f;
        if (isTurningDirection)
        {
            return turnAcceleration;
        }

        // Case 3) 기존 방향으로 계속 이동하는 경우
        return moveAcceleration;
    }

    // 공중에 있고 이동하려는 방향의 벽과 접촉한 경우에 한해 true 반환.
    public bool ShouldStickToWall(float moveInput)
    {
        // TODO: 이미 한 번 붙었다가 떨어진 벽에는 다시 붙을 수 없도록 제한 (무한 벽타기 방지)
        bool shouldStickRight = moveInput > 0f && groundContact.IsInContactWithRightWall;
        bool shouldStickLeft = moveInput < 0f && groundContact.IsInContactWithLeftWall;
        return !IsGrounded && (shouldStickRight || shouldStickLeft);
    }

    // 벽에 붙은 방향과 반대로 이동하는 경우 벽붙기 중지
    public bool ShouldStopStickingToWall(float moveInput)
    {
        return
            (isStickingToRightWall && moveInput < 0f) || // 오른쪽 벽에 붙은 상태에서 왼쪽으로 이동
            (!isStickingToRightWall && moveInput > 0f); // 왼쪽 벽에 붙은 상태에서 오른쪽으로 이동
    }

    public void StartStickingToWall(float moveInput)
    {
        // 매달린 방향과 반대로 이동하는 경우 매달리기 취소해야 하므로 현재 방향 기록
        isStickingToRightWall = moveInput > 0f;

        // wall jump 가능하게 설정
        jumpCount = 0;
        shouldWallJump = true;

        // coyote time 리셋
        coyoteTimeCounter = 0f;

        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;

        // TODO: 벽에 붙어도 공중 공격 가능 여부를 초기화해야 한다면 isAirAttackPossible = true 넣기
    }

    public void StopStickingToWall()
    {
        // 중력 활성화
        rb.gravityScale = defaultGravityScale;
    }

    public bool TryJump()
    {
        // 점프에는 두 가지 경우가 있음
        // 1. 1차 점프 - 플랫폼과 접촉한 경우 또는 coyote time이 아직 유효한 경우
        // 2. 2차 점프 - 이미 점프한 경우 또는 coyote time이 유효하지 않은 경우
        if (IsInitialJump())
        {
            jumpCount = 1;
            PerformJump();

            return true;
        }
        else if (IsDoubleJump())
        {
            // TODO: 더블 점프는 로그 타입만 가능하도록 수정
            jumpCount = 2;
            PerformJump();
            
            return true;
        }

        return false;
    }

    
    // 최초의 점프로 취급하는 경우
    // 1. 바닥에 서있는 경우
    // 2. 벽에 매달려있는 경우
    // 3. 1또는 2의 상황에서 추락을 시작한지 얼마 지나지 않은 경우 (coyote time 유효)
    private bool IsInitialJump()
    {
        return jumpCount == 0 && coyoteTimeCounter < coyoteTime;
    }

    // 더블 점프로 취급하는 경우
    // 1. 이미 최초의 점프를 완료한 경우
    // 2. 아직 점프를 하지는 않았지만 추락 시간이 허용된 coyote time을
    //    초과해서 공중에 떠있는 것으로 취급하는 경우
    private bool IsDoubleJump()
    {
        // 더블 점프는 일단 폐기...
        return false;
        //return jumpCount == 1 || (jumpCount == 0 && coyoteTimeCounter > coyoteTime);
    }

    private void PerformJump()
    {
        // 혹시 엘리베이터 위에 있었다면 움직임을 동기화하는 기능이
        // 점프를 방해하게 되니 이를 먼저 취소해줘야 함
        StopStickingToMovingPlatform();

        // 지금 벽에 매달려있거나 방금까지 벽에 매달려있던 경우 (coyote time) wall jump로 전환
        if (shouldWallJump)
        {
            // 더블 점프에서도 wall jump가 실행되는 것 방지
            shouldWallJump = false;

            StopStickingToWall();

            ApplyWallJumpAirControlForDurationAsync(wallJumpAirControlPenaltyDuration).Forget();

            // wallJumpVelocity는 오른쪽으로 박차고 나가는 기준이라서
            // 왼쪽으로 가야 하는 경우 x축 속도를 반전시켜야 함.
            rb.velocity = new(wallJumpVelocity.x * (isStickingToRightWall ? -1f : 1f), wallJumpVelocity.y);
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
        }

        // Coyote time에 점프한 경우 중력이 gravityScaleWhenFalling으로
        // 설정되어 있으므로 점프 시 중력으로 덮어쓰기.
        rb.gravityScale = defaultGravityScale;
        
        // 경사로를 타고 올라가다가 점프하면 바로 착지해버리는 상황을
        // 잠시동안 지면 검출을 멈추는 방식으로 방지함.
        groundContact.PreventTestForDuration(0.1f);
    }

    // wall jump 직후에 너무 빨리 벽으로 돌아오는 것을
    // 막기 위해 잠시 더 낮은 airControl 수치를 적용함.
    private async UniTask ApplyWallJumpAirControlForDurationAsync(float duration)
    {
        airControl = airControlWhileWallJumping;

        await UniTask.WaitForSeconds(duration);

        airControl = defaultAirControl;
    }

    // 주어진 방향이 낭떠러지인지 반환
    public bool IsOnBrink(float direction)
    {
        return 
            (direction > 0f && !groundContact.IsRightFootGrounded) ||
            (direction < 0f && !groundContact.IsLeftFootGrounded);
    }

    public void PerformDash(bool isFacingLeft)
    {
        // 엘리베이터 위에 있으면 kinematic rigidbody로 변하므로
        // 이대로 두면 벽을 뚫고 지나가버릴 위험이 있음!
        if (IsStickingToMovingPlatform)
        {
            StopStickingToMovingPlatform();
        }

        // 회피 도중에는 추락 및 넉백 x
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        // 지면에 멈춰있는 상태에서는 엄청 큰 마찰력이 사용되므로
        // 확실히 마찰력을 없애주지 않으면 땅 위에 가만히 멈춰있을 위험이 있음.
        ApplyZeroFriction();

        float targetX = transform.position.x + evasionDistance * (isFacingLeft ? -1f : 1f);
        rb.DOMoveX(targetX, evasionDuration)
            .SetEase(evasionEase)
            .SetUpdate(UpdateType.Fixed);
    }
}
