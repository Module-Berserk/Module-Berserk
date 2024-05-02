using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Movement Stats")]
    [SerializeField] private float MaxMoveSpeed = 1.5f;
    [SerializeField] private float TurnAcceleration = 60f;
    [SerializeField] private float MoveAcceleration = 30f;
    [SerializeField] private float MoveDecceleration = 50f;
    [SerializeField] private float JumpForce = 3.5f;
    [SerializeField] private float CoyoteTime = 0.15f;
    [SerializeField, Range(0f, 1f)] private float AirControl = 0.5f;

    [Header("Ground Contact")]
    [SerializeField] private LayerMask groundLayerMask;

    // 컴포넌트 레퍼런스
    private PlayerActionAssets actionAssets;
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;

    // FixedUpdate()에서 땅을 밟고 있는지 확인하고 여기에 기록함
    private bool isGrounded;

    // 땅에서 떨어진 시점부터 Time.deltaTime을 누적하는 카운터로,
    // 이 값이 CoyoteTime보다 낮을 경우 isGrounded가 false여도 점프 가능.
    private float coyoteTimeCounter;

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
        CheckIsGrounded();
        if (isGrounded)
        {
            ResetJumpAndCoyoteTime();
        }
        else
        {
            HandleCoyoteTime();
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

    private void ResetJumpAndCoyoteTime()
    {
        canJump = true;
        coyoteTimeCounter = 0f;
    }

    private void HandleCoyoteTime()
    {
        coyoteTimeCounter += Time.fixedDeltaTime;
        if (coyoteTimeCounter > CoyoteTime)
        {
            canJump = false;
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

            rb.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
        }

        // 입력 처리 완료
        isJumpKeyPressed = false;
    }
}
