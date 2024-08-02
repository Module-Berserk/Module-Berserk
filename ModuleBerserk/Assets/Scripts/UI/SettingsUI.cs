using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 일시정지 메뉴의 설정창에서 일어나는 모든 일을 관리하는 클래스.
// 화면 해상도, 볼륨, 화면 흔들림 토글 등의 옵션이 있다.
public class SettingsUI : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private Toggle fullScreenToggle;

    private void OnEnable()
    {
        UserInterfaceStack.PushUserInterface(this, fullScreenToggle.gameObject);

        fullScreenToggle.isOn = Screen.fullScreen;
    }

    private void OnDisable()
    {
        UserInterfaceStack.PopUserInterface(this);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        Debug.Log($"전체화면 변경 결과: {Screen.fullScreen}");
    }

    public void HideSettingsUI()
    {
        gameObject.SetActive(false);
    }

    private void OnEscapeKey(InputAction.CallbackContext context)
    {
        HideSettingsUI();
    }

    void IUserInterfaceController.BindInputActions()
    {
        InputManager.InputActions.Common.Escape.performed += OnEscapeKey;

        fullScreenToggle.interactable = true;
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        InputManager.InputActions.Common.Escape.performed -= OnEscapeKey;

        fullScreenToggle.interactable = false;
    }
}
