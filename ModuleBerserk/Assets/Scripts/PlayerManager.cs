using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Walk / Run")]
    [SerializeField] private float MaxMoveSpeed = 1.5f;
    [SerializeField] private float TurnAcceleration = 60f;
    [SerializeField] private float MoveAcceleration = 30f;
    [SerializeField] private float MoveDecceleration = 50f;


    [Header("Jump / Fall")]
    [SerializeField] private float JumpVelocity = 4f;
    // 땅에서 떨어져도 점프를 허용하는 time window
    [SerializeField] private float CoyoteTime = 0.15f;
    // 공중에 있지만 위로 이동하는 중이라면 DefaultGravityScale을 사용하고
    // 아래로 이동하는 중이라면 GravityScaleWhenFalling을 사용해
    // 더 빨리 추락해서 공중에 붕 뜨는 이상한 느낌을 줄임.
    [SerializeField] private float DefaultGravityScale = 1f;
    [SerializeField] private float GravityScaleWhenFalling = 1.7f;
    // 아주 높은 곳에서 떨어질 때 부담스러울 정도로 아래로 가속하는 상황 방지
    [SerializeField] private float MaxFallSpeed = 5f;
    // 공중 조작이 지상 조작보다 둔하게 만드는 파라미터 (0: 조작 불가, 1: 변화 없음)
    [SerializeField, Range(0f, 1f)] private float AirControl = 0.5f;


    [Header("Ground Contact")]
    // 땅으로 취급할 layer를 모두 에디터에서 지정해줘야 함!
    [SerializeField] private LayerMask groundLayerMask;


    // 컴포넌트 레퍼런스
    private PlayerActionAssets actionAssets;
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider; // TODO: 캡슐 대신 그냥 box collider를 사용할지 결정하기


    // FixedUpdate()에서 땅을 밟고 있는지 확인하고 여기에 기록함
    private bool isGrounded;
    // 땅에서 떨어진 시점부터 Time.deltaTime을 누적하는 카운터로,
    // 이 값이 CoyoteTime보다 낮을 경우 isGrounded가 false여도 점프 가능.
    private float coyoteTimeCounter;
    // coyote time, 더블 점프 등을 모두 고려한 점프 가능 여부로,
    // FixedUpdate()에서 업데이트됨.
    private bool canJump = true;
    // 키 입력은 physics 루프와 다른 시점에 처리되니까
    // 여기에 기록해두고 물리 연산은 FixedUpdate에서 처리함
    private bool isJumpKeyPressed;

    private void Awake()
    {
        FindComponentReferences();
        BindInputActions();
    }

    private void FindComponentReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
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
        // TODO: 아래에 OneWayPlatform이 있으면 잠시 플랫폼과 플레이어의 collision 비활성화
        Debug.Log("OnFallDown");
    }

    private void FixedUpdate()
    {
        // TODO: 공중에서 벽에 닿으면 달라붙는 기능 구현하기

        CheckIsGrounded();
        if (isGrounded)
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
    }

    // Capsule의 양 끝에서 아래로 height의 절반 만큼 raycast해서 지금 땅을 밟고있는지 체크.
    // 중앙에서 raycast하면 플랫폼 가장자리에 있을 때 false가 나와버리니 반드시 양쪽을 모두 체크해줘야 함.
    private void CheckIsGrounded()
    {
        // 점프하면서 one way platform을 관통하다가 raycast가 성공할 수도 있으니
        // 위로 이동하는 중이라면 무조건 isGrounded = false로 설정하고 바로 종료.
        if (rb.velocity.y > 0.01f)
        {
            isGrounded = false;
            return;
        }

        // 콜라이더의 중심
        Vector2 center = transform.position;
        center += capsuleCollider.offset;

        // 콜라이더의 양 끝까지의 displacement
        float halfWidth = capsuleCollider.size.x / 2f;
        Vector2 sideOffset = new(halfWidth, 0f);

        // 정확히 half height만큼 하면 땅에 서있어도 fasle 나올 수 있으니 약간 여유 주기.
        float halfHeight = capsuleCollider.size.y / 2f;
        float traceDistance = halfHeight + 0.02f;

        isGrounded =
            Physics2D.Raycast(center + sideOffset, Vector2.down, traceDistance, groundLayerMask) ||
            Physics2D.Raycast(center - sideOffset, Vector2.down, traceDistance, groundLayerMask);

        // 나중에 시각적으로 trace 범위를 확인하고 싶을 수도 있으니 주석으로 남겨둠.
        // Debug.DrawLine(center + sideOffset, center + sideOffset + Vector2.down * traceDistance);
        // Debug.DrawLine(center - sideOffset, center - sideOffset + Vector2.down * traceDistance);
    }

    private void ResetJumpRelatedStates()
    {
        canJump = true;
        coyoteTimeCounter = 0f;
        rb.gravityScale = DefaultGravityScale;
    }

    private void HandleCoyoteTime()
    {
        coyoteTimeCounter += Time.fixedDeltaTime;
        if (coyoteTimeCounter > CoyoteTime)
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
            rb.gravityScale = GravityScaleWhenFalling;
        }

        // 최대 추락 속도 제한
        if (rb.velocity.y < -MaxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -MaxFallSpeed);
        }
    }

    private void HandleMoveInput()
    {
        // 원하는 속도를 계산
        float moveInput = actionAssets.Player.Move.ReadValue<float>();
        float desiredVelocityX = MaxMoveSpeed * moveInput * MaxMoveSpeed;

        // 방향 전환 여부에 따라 다른 가속도 사용
        float acceleration = ChooseAcceleration(moveInput, desiredVelocityX);

        // 공중이라면 AirControl 수치(0.0 ~ 1.0)에 비례해 가속도 감소
        if (!isGrounded)
        {
            acceleration *= AirControl;
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
            return MoveDecceleration;
        }

        // Case 2) 반대 방향으로 이동하려는 경우
        bool isTurningDirection = rb.velocity.x * desiredVelocityX < 0f;
        if (isTurningDirection)
        {
            return TurnAcceleration;
        }

        // Case 3) 기존 방향으로 계속 이동하는 경우
        return MoveAcceleration;
    }

    private void HandleJumpInput()
    {
        if (canJump)
        {
            // 더블 점프 방지
            canJump = false;

            // TODO: 벽에 달라붙은 상태라면 벽과 반대 방향으로 점프하도록 구현
            rb.velocity = new Vector2(rb.velocity.x, JumpVelocity);
        }

        // 입력 처리 완료
        isJumpKeyPressed = false;
    }
}
