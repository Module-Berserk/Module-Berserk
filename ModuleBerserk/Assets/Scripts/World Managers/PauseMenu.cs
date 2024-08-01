using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 일시정지 메뉴를 띄우는 클래스
public class PauseMenu : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private YesNoSelectionUI warningUI;
    [SerializeField] private FadeEffect fadeEffect;

    // 일시정지가 끝날 때 플레이어 조작 입력을 활성화해야할지 결정하는 플래그.
    // 의뢰 UI 등 플레이어 조작이 이미 막힌 상태에서 일시정지를 할 수도 있으므로
    // 일시정지 해제 시점에 무작정 플레이어 입력을 활성화하면 안된다!
    private bool wasPlayerInputEnabled;

    private void OnEnable()
    {
        InputManager.InputActions.Common.Escape.performed += TogglePauseMenu;

        continueButton.onClick.AddListener(DisablePauseMenu);
        mainMenuButton.onClick.AddListener(OnMainMenuButtonClick);
        quitGameButton.onClick.AddListener(OnQuitGameButtonClick);
    }

    private void OnDisable()
    {
        continueButton.onClick.RemoveListener(DisablePauseMenu);
        mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClick);
        quitGameButton.onClick.RemoveListener(OnQuitGameButtonClick);
    }

    private void OnMainMenuButtonClick()
    {
        warningUI.gameObject.SetActive(true);
        warningUI.OnSelect.AddListener((bool isYesClicked) => {
            if (isYesClicked)
            {
                ReturnToMainMenuAsync().Forget();
            }
        });
    }

    private void OnQuitGameButtonClick()
    {
        warningUI.gameObject.SetActive(true);
        warningUI.OnSelect.AddListener((bool isYesClicked) => {
            if (isYesClicked)
            {
                Application.Quit();
            }
        });
    }

    private async UniTask ReturnToMainMenuAsync()
    {
        warningUI.gameObject.SetActive(false);
        DisablePauseMenu();
        enabled = false;

        fadeEffect.FadeOut(ignoreTimeScale: true);

        await UniTask.WaitForSeconds(1f, ignoreTimeScale: true);
        await SceneManager.LoadSceneAsync("Title");

        // 아이템 교체 UI, 컷신 등으로 인해 입력이 비활성화된 상태일 수도 있으니
        // 영원히 입력이 막히지 않도록 안전하게 풀어줌.
        InputManager.InputActions.Player.Enable();
    }

    private void TogglePauseMenu(InputAction.CallbackContext context)
    {
        if (pauseMenuUI.activeInHierarchy)
        {
            DisablePauseMenu();
        }
        else
        {
            EnablePauseMenu();
        }
    }

    private void DisablePauseMenu()
    {
        pauseMenuUI.SetActive(false);

        UserInterfaceStack.PopUserInterface(this);
        TimeManager.ResumeGame();

        // 일시정지를 누를 때 플레이어 입력이 활성화 상태였다면 복구
        if (wasPlayerInputEnabled)
        {
            InputManager.InputActions.Player.Enable();
        }

        // 다시 게임을 재개해도 언제든 일시정지 메뉴를 불러올 수 있도록 다시 콜백 등록
        InputManager.InputActions.Common.Escape.performed += TogglePauseMenu;
    }

    private void EnablePauseMenu()
    {
        // 일시정지 메뉴가 떠있는 동안은 자신이 최상단 UI인 순간에만 반응하도록
        // 콜백을 BindInputActions()와 UnbindInputActions()에서 처리함.
        //
        // 평소에도 일시정지 키에 반응하도록 상시 대기중인 콜백이 하나 존재하므로
        // 콜백 중복을 막기 위해 여기서 기존 콜백을 제거해줘야 함.
        InputManager.InputActions.Common.Escape.performed -= TogglePauseMenu;

        pauseMenuUI.SetActive(true);

        UserInterfaceStack.PushUserInterface(this, continueButton.gameObject);
        TimeManager.PauseGame();

        // 일시정지를 누를 때 플레이어 입력이 활성화 상태라면 기록해두고 잠깐 비활성화
        wasPlayerInputEnabled = InputManager.InputActions.Player.enabled;
        if (wasPlayerInputEnabled)
        {
            InputManager.InputActions.Player.Disable();
        }
    }

    void IUserInterfaceController.BindInputActions()
    {
        InputManager.InputActions.Common.Escape.performed += TogglePauseMenu;

        continueButton.interactable = true;
        settingsButton.interactable = true;
        mainMenuButton.interactable = true;
        quitGameButton.interactable = true;
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        InputManager.InputActions.Common.Escape.performed -= TogglePauseMenu;

        continueButton.interactable = false;
        settingsButton.interactable = false;
        mainMenuButton.interactable = false;
        quitGameButton.interactable = false;
    }
}
