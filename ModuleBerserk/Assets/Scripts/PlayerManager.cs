using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Walk / Run")]
    [SerializeField] private float maxMoveSpeed = 1.5f;
    [SerializeField] private float turnAcceleration = 60f;
    [SerializeField] private float moveAcceleration = 30f;
    [SerializeField] private float moveDecceleration = 50f;


    [Header("Jump / Fall")]
    [SerializeField] private float jumpVelocity = 4f;
    // 땅에서 떨어져도 점프를 허용하는 time window
    [SerializeField] private float coyoteTime = 0.15f;
    // 공중에 있지만 위로 이동하는 중이라면 DefaultGravityScale을 사용하고
    // 아래로 이동하는 중이라면 GravityScaleWhenFalling을 사용해
    // 더 빨리 추락해서 공중에 붕 뜨는 이상한 느낌을 줄임.
    [SerializeField] private float defaultGravityScale = 1f;
    [SerializeField] private float gravityScaleWhenFalling = 1.7f;
    // 아주 높은 곳에서 떨어질 때 부담스러울 정도로 아래로 가속하는 상황 방지
    [SerializeField] private float maxFallSpeed = 5f;
    // 공중 조작이 지상 조작보다 둔하게 만드는 파라미터 (0: 조작 불가, 1: 변화 없음)
    [SerializeField, Range(0f, 1f)] private float airControl = 0.5f;


    [Header("Ground Contact")]
    // 땅으로 취급할 layer를 모두 에디터에서 지정해줘야 함!
    [SerializeField] private LayerMask groundLayerMask;
    // 콜라이더로부터 이 거리보다 가까우면 접촉 중이라고 취급
    [SerializeField] private float contactDistanceThreshold = 0.02f;

    [Header("Follow Camera Target")]
    // Cinemachine Virtual Camera의 Follow로 등록된 오브젝트를 지정해줘야 함!
    // 새로운 높이의 플랫폼에 착지하기 전까지 카메라의 y축 좌표를 일정하게 유지하는 용도.
    [SerializeField] private GameObject cameraFollowTarget;
    // 바라보는 방향으로 얼마나 앞에 있는 지점을 카메라가 추적할 것인지
    [SerializeField, Range(0f, 2f)] private float cameraLookAheadDistance = 1f;


    // 컴포넌트 레퍼런스
    private PlayerActionAssets actionAssets;
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider; // TODO: 캡슐 대신 그냥 box collider를 사용할지 결정하기
    private SpriteRenderer spriteRenderer;

    // 지면 접촉 테스트 관리자
    private GroundContact groundContact;
    // 땅에서 떨어진 시점부터 Time.deltaTime을 누적하는 카운터로,
    // 이 값이 CoyoteTime보다 낮을 경우 isGrounded가 false여도 점프 가능.
    private float coyoteTimeCounter = 0f;
    // coyote time, 더블 점프 등을 모두 고려한 점프 가능 여부로,
    // FixedUpdate()에서 업데이트됨.
    private bool canJump = true;
    // 키 입력은 physics 루프와 다른 시점에 처리되니까
    // 여기에 기록해두고 물리 연산은 FixedUpdate에서 처리함
    private bool isJumpKeyPressed = false;
    // 현재 어느 쪽을 바라보고 있는지 기록.
    // 스프라이트 반전과 카메라 추적 위치 조정에 사용됨.
    private bool isFacingRight = true;

    private void Awake()
    {
        FindComponentReferences();
        BindInputActions();

        groundContact = new(rb, capsuleCollider, groundLayerMask, contactDistanceThreshold);
    }

    private void FindComponentReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void BindInputActions()
    {
        actionAssets = new PlayerActionAssets();
        actionAssets.Player.Enable();

        actionAssets.Player.Jump.performed += OnJump;
        actionAssets.Player.FallDown.performed += OnFallDown;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        isJumpKeyPressed = true;
    }

    private void OnFallDown(InputAction.CallbackContext context)
    {
        if (groundContact.IsSteppingOnOneWayPlatform())
        {
            // await 없이 비동기로 처리하기 위해 discard
            _ = groundContact.IgnoreCurrentPlatformForDurationAsync(0.5f);
        }
    }

    private void FixedUpdate()
    {
        groundContact.TestContact();
        if (groundContact.IsGrounded)
        {
            ResetJumpRelatedStates();
        }
        else
        {
            HandleCoyoteTime();
            HandleFallingVelocity();
        }

        HandleMoveInput();

        if (isJumpKeyPressed)
        {
            HandleJumpInput();
        }

        UpdateCameraFollowTarget();
        UpdateSpriteDirection();
    }

    private void ResetJumpRelatedStates()
    {
        canJump = true;
        coyoteTimeCounter = 0f;
        rb.gravityScale = defaultGravityScale;
    }

    private void HandleCoyoteTime()
    {
        coyoteTimeCounter += Time.fixedDeltaTime;
        if (coyoteTimeCounter > coyoteTime)
        {
            canJump = false;
        }
    }
    
    private void HandleFallingVelocity()
    {
        // 현재 추락하는 중이라면 더 강한 중력을 사용해서 붕 뜨는 느낌을 줄임.
        bool isFalling = rb.velocity.y < -0.01f;
        if (isFalling)
        {
            rb.gravityScale = gravityScaleWhenFalling;
        }

        // 최대 추락 속도 제한
        if (rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }
    }

    private void HandleMoveInput()
    {
        // 원하는 속도를 계산
        float moveInput = actionAssets.Player.Move.ReadValue<float>();
        float desiredVelocityX = maxMoveSpeed * moveInput * maxMoveSpeed;

        // 현재 방향 기록 (입력이 없는 경우 현재 방향 유지)
        if (moveInput != 0f)
        {
            isFacingRight = moveInput > 0f;
        }

        // 방향 전환 여부에 따라 다른 가속도 사용
        float acceleration = ChooseAcceleration(moveInput, desiredVelocityX);

        // 공중이라면 AirControl 수치(0.0 ~ 1.0)에 비례해 가속도 감소
        if (!groundContact.IsGrounded)
        {
            acceleration *= airControl;
        }

        // x축 속도가 원하는 속도에 부드럽게 도달하도록 보간
        float updatedVelocityX = Mathf.MoveTowards(rb.velocity.x, desiredVelocityX, acceleration * Time.deltaTime);
        rb.velocity = new Vector2(updatedVelocityX, rb.velocity.y);
    }

    private float ChooseAcceleration(float moveInput, float desiredVelocityX)
    {
        // Case 1) 이동을 멈추는 경우
        bool isStopping = moveInput == 0f;
        if (isStopping)
        {
            return moveDecceleration;
        }

        // Case 2) 반대 방향으로 이동하려는 경우
        bool isTurningDirection = rb.velocity.x * desiredVelocityX < 0f;
        if (isTurningDirection)
        {
            return turnAcceleration;
        }

        // Case 3) 기존 방향으로 계속 이동하는 경우
        return moveAcceleration;
    }

    private void HandleJumpInput()
    {
        if (canJump)
        {
            // 더블 점프 방지
            canJump = false;

            // TODO: 벽에 달라붙은 상태라면 벽과 반대 방향으로 점프하도록 구현
            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);

            // Coyote time에 점프한 경우 중력이 gravityScaleWhenFalling으로
            // 설정되어 있으므로 점프 시 중력으로 덮어쓰기.
            rb.gravityScale = defaultGravityScale;
        }

        // 입력 처리 완료
        isJumpKeyPressed = false;
    }

    // 평지에서 점프할 때 카메라가 위 아래로 흔들리는 것을 방지하기 위해
    // 카메라 추적 대상을 플레이어와 별개의 오브젝트로 설정하고
    // 플랫폼에 착지했을 때만 플레이어의 y 좌표를 따라가도록 설정함.
    // x 좌표의 경우 플레이어의 실시간 위치 + 바라보는 방향으로 look ahead.
    //
    // Note:
    // 맵이 수직으로 그리 높지 않은 경우는 괜찮은데
    // 절벽처럼 카메라 시야를 벗어날 정도로 낙하하는 경우에는
    // 캐릭터가 갑자기 화면 밖으로 사라지니까 이상하다고 느낄 수 있음.
    private void UpdateCameraFollowTarget()
    {
        Vector2 newPosition = transform.position;

        // 바라보는 방향으로 look ahead
        //
        // 개인적인 의견: 좌/우 번갈아가면서 입력하면 카메라가 이리저리 이동해서 너무 어지러움...
        newPosition.x += isFacingRight ? cameraLookAheadDistance : -cameraLookAheadDistance;

        // 아직 새로운 플랫폼에 착지하지 않았다면 y 좌표는 유지.
        if (!groundContact.IsGrounded)
        {
            newPosition.y = cameraFollowTarget.transform.position.y;
        }

        cameraFollowTarget.transform.position = newPosition;
    }

    private void UpdateSpriteDirection()
    {
        spriteRenderer.flipX = !isFacingRight;
    }
}
