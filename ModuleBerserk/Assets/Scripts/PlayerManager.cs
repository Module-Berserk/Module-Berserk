using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    private PlayerActionAssets actionAssets;

    private void Awake()
    {
        BindInputActions();
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
        // TODO: Move.ReadValue<float>()로 가속/감속
    }
}
