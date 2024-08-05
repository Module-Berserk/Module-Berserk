using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TitleSceneController : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private TextMeshProUGUI pressAnyKeyText;
    [SerializeField] private GameObject secondaryUI;


    private void Start()
    {
        pressAnyKeyText.DOFade(0.5f, 2f)
            .From(0f)
            .SetEase(Ease.InSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDestroy()
    {
        pressAnyKeyText.DOKill();
    }

    private void OnEnable()
    {
        // 타이틀 씬에는 "아무 키나 누르세요" 글씨를 제외하면 실질적인 UI 요소가 없음!
        // 따라서 UI navigation에 필요한 값인 두 번째 파라미터는 null이어도 된다.
        UserInterfaceStack.PushUserInterface(this, firstSelectedUIElement: null);
    }

    private void OnDisable()
    {
        UserInterfaceStack.PopUserInterface(this);
    }

    void IUserInterfaceController.BindInputActions()
    {
        InputManager.InputActions.UI.AnyKey.performed += ShowSecondaryUI;
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        InputManager.InputActions.UI.AnyKey.performed -= ShowSecondaryUI;
    }

    private void ShowSecondaryUI(InputAction.CallbackContext context)
    {
        // 새로하기 또는 이어하기를 눌러서 맵이 로딩되는 도중에
        // 아무키나 누르면 2차 UI가 다시 나와버리므로 여기서 비활성화
        enabled = false;

        secondaryUI.SetActive(true);
    }
}