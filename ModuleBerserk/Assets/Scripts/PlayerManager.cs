using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Movement Stats")]
    [SerializeField] private float MaxMoveSpeed = 1.5f;
    [SerializeField] private float TurnAcceleration = 60f;
    [SerializeField] private float MoveAcceleration = 30f;
    [SerializeField] private float MoveDecceleration = 50f;

    private PlayerActionAssets actionAssets;
    private Rigidbody2D rb;

    private void Awake()
    {
        FindComponentReferences();
        BindInputActions();
    }

    private void FindComponentReferences()
    {
        rb = GetComponent<Rigidbody2D>();
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
        // TODO: 바닥을 밟고 있으면 위로 AddForce
        Debug.Log("OnJump");
    }

    private void OnFallDown(InputAction.CallbackContext context)
    {
        // TODO: 아래에 OneWayPlatform이 있으면 잠시 플랫폼과 플레이어의 collision 비활성화
        Debug.Log("OnFallDown");
    }

    private void FixedUpdate()
    {
        HandleMoveInput();
    }

    private void HandleMoveInput()
    {
        // 원하는 속도를 계산
        float moveInput = actionAssets.Player.Move.ReadValue<float>();
        float desiredVelocityX = MaxMoveSpeed * moveInput * MaxMoveSpeed;

        // 방향 전환 여부에 따라 다른 가속도 사용
        float acceleration = ChooseAcceleration(moveInput, desiredVelocityX);

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
}
