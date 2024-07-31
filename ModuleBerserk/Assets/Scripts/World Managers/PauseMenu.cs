using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 일시정지 메뉴를 띄우는 클래스
public class PauseMenu : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private List<Button> buttons;

    private bool isPaused = false;

    // 일시정지가 끝날 때 플레이어 조작 입력을 활성화해야할지 결정하는 플래그.
    // 의뢰 UI 등 플레이어 조작이 이미 막힌 상태에서 일시정지를 할 수도 있으므로
    // 일시정지 해제 시점에 무작정 플레이어 입력을 활성화하면 안된다!
    private bool wasPlayerInputEnabled;

    private void OnEnable()
    {
        InputManager.InputActions.Common.Escape.performed += TogglePauseMenu;
    }

    private void OnDisable()
    {
        InputManager.InputActions.Common.Escape.performed -= TogglePauseMenu;
    }

    private void TogglePauseMenu(InputAction.CallbackContext context)
    {
        isPaused = !isPaused;

        pauseMenuUI.SetActive(isPaused);

        if (isPaused)
        {
            UserInterfaceStack.PushUserInterface(this);
            TimeManager.PauseGame();

            // 일시정지를 누를 때 플레이어 입력이 활성화 상태라면 기록해두고 잠깐 비활성화
            wasPlayerInputEnabled = InputManager.InputActions.Player.enabled;
            if (wasPlayerInputEnabled)
            {
                InputManager.InputActions.Player.Disable();
            }
        }
        else
        {
            UserInterfaceStack.PopUserInterface(this);
            TimeManager.ResumeGame();

            // 일시정지를 누를 때 플레이어 입력이 활성화 상태였다면 복구
            if (wasPlayerInputEnabled)
            {
                InputManager.InputActions.Player.Enable();
            }
        }
    }

    void IUserInterfaceController.BindInputActions()
    {
        // TODO: 일시정지 메뉴 만들 때 여기서 입력 binding 처리할 것
        foreach (var button in buttons)
        {
            button.interactable = true;
        }

        EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        // TODO: 일시정지 메뉴 만들 때 여기서 입력 unbinding 처리할 것
        foreach (var button in buttons)
        {
            button.interactable = false;
        }
    }
}
