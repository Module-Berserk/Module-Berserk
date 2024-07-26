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
        UserInterfaceStack.PushUserInterface(this);
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
        secondaryUI.SetActive(true);
    }
}
