using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

public class TitleSceneSelectSaveDataUI : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private List<Button> buttons;

    public UnityEvent<int> OnSelectExistingSlot = new();
    public UnityEvent<int> OnSelectEmptySlot = new();

    private void Awake()
    {
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        // 버튼 수와 세이브 데이터 슬롯 수는 항상 일치해야 함
        List<GameState> savedStates = GameStateManager.LoadSavedGameStates();
        Assert.IsTrue(savedStates.Count == buttons.Count);

        for (int i = 0; i < savedStates.Count; ++i)
        {
            int buttonIndex = i; // 람다에 i를 직접 넣으니 루프 끝난 i값이 사용되어서 참조용 로컬 변수를 하나 더 만듦
            if (savedStates[i] == null)
            {
                SetButtonLocalizationSmartString(buttons[i], -1);
                buttons[i].onClick.AddListener(() =>
                {
                    OnSelectEmptySlot.Invoke(buttonIndex);
                });
            }
            else
            {
                SetButtonLocalizationSmartString(buttons[i], i);
                buttons[i].onClick.AddListener(() => {
                    OnSelectExistingSlot.Invoke(buttonIndex);
                });
            }
        }
    }

    // localization에서 slotIndex라는 로컬 변수를 사용해
    // 언어에 맞게 formatting 해주므로 우리는 해당 값만 설정해주면 됨.
    // -1은 빈 슬롯이라는 뜻!
    private void SetButtonLocalizationSmartString(Button button, int slotIndex)
    {
        if (button.GetComponentInChildren<LocalizeStringEvent>().StringReference.TryGetValue("slotIndex", out IVariable variable))
        {
            (variable as IntVariable).Value = slotIndex;
        }
    }

    private void OnEnable()
    {
        UserInterfaceStack.PushUserInterface(this, buttons[0].gameObject);
    }

    private void OnDisable()
    {
        UserInterfaceStack.PopUserInterface(this);
    }

    private void DisableSelf(InputAction.CallbackContext context)
    {
        gameObject.SetActive(false);

        OnSelectExistingSlot.RemoveAllListeners();
        OnSelectEmptySlot.RemoveAllListeners();
    }

    void IUserInterfaceController.BindInputActions()
    {
        InputManager.InputActions.Common.Escape.performed += DisableSelf;

        foreach (var button in buttons)
        {
            button.interactable = true;
        }
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        InputManager.InputActions.Common.Escape.performed -= DisableSelf;

        foreach (var button in buttons)
        {
            button.interactable = false;
        }
    }
}
