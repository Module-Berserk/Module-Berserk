using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
            int buttonIndex = i; // 람다에 i를 직접 넣으니 루프 끝난 i값이 사용되어서
            if (savedStates[i] == null)
            {
                buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = "빈 슬롯";
                buttons[i].onClick.AddListener(() => {
                    OnSelectEmptySlot.Invoke(buttonIndex);
                });
            }
            else
            {
                buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = $"슬롯 {i + 1}";
                buttons[i].onClick.AddListener(() => {
                    OnSelectExistingSlot.Invoke(buttonIndex);
                });
            }
        }
    }

    private void OnEnable()
    {
        UserInterfaceStack.PushUserInterface(this);
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
        
        EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
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
