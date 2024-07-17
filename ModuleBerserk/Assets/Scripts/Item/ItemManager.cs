using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

// 필드에서 습득하는 아이템을 슬롯에 저장하고 사용하는 일을 담당하는 클래스
public class ItemManager : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private GameObject itemReplacementUI;
    [SerializeField] private GameObject itemReplacementSlot1Outline;
    [SerializeField] private GameObject itemReplacementSlot2Outline;

    // 아이템 교체 UI에서 현재 어느 슬롯을 교체 대상으로 지정하고 있는지
    private bool isSelectingSlot1ForReplacement = true;
    // 새로 습득해서 교체하려는 아이템
    private IActiveItem replacementPendingItem = null;

    // 각 슬롯에 들어있는 아이템
    private IActiveItem slot1 = null;
    private IActiveItem slot2 = null;

    void IUserInterfaceController.BindInputActions()
    {
        var uiActions = InputManager.InputActions.UI;
        uiActions.Select.performed += SelectItemReplacementSlot;
        uiActions.Left.performed += ChangeItemReplacementSlot;
        uiActions.Right.performed += ChangeItemReplacementSlot;
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        var uiActions = InputManager.InputActions.UI;
        uiActions.Select.performed -= SelectItemReplacementSlot;
        uiActions.Left.performed -= ChangeItemReplacementSlot;
        uiActions.Right.performed -= ChangeItemReplacementSlot;
    }

    private void SelectItemReplacementSlot(InputAction.CallbackContext context)
    {
        Assert.IsTrue(itemReplacementUI.activeInHierarchy);

        // 아이템 교체 UI 숨기기
        UserInterfaceStack.PopUserInterface(this);
        itemReplacementUI.SetActive(false);

        // 다시 플레이어 조작 활성화
        InputManager.InputActions.Player.Enable();

        // UI에서 선택한 슬롯의 내용물 교체
        if (isSelectingSlot1ForReplacement)
        {
            slot1 = replacementPendingItem;
        }
        else
        {
            slot2 = replacementPendingItem;
        }
    }

    private void ChangeItemReplacementSlot(InputAction.CallbackContext context)
    {
        // 선택하지 않은 슬롯으로 전환
        isSelectingSlot1ForReplacement = !isSelectingSlot1ForReplacement;

        // 활성화된 슬롯 쪽의 테두리만 표시
        itemReplacementSlot1Outline.SetActive(isSelectingSlot1ForReplacement);
        itemReplacementSlot2Outline.SetActive(!isSelectingSlot1ForReplacement);
    }

    public void HandleItemCollect(IActiveItem item)
    {
        if (slot1 == null)
        {
            slot1 = item;
        }
        else if (slot2 == null)
        {
            slot2 = item;
        }
        else
        {
            ShowItemReplacementUI(item);
        }
    }

    private void ShowItemReplacementUI(IActiveItem newItem)
    {
        UserInterfaceStack.PushUserInterface(this);

        // 잠시 플레이어 조작 비활성화
        InputManager.InputActions.Player.Disable();

        // 교체 대상인 아이템 잠시 저장
        replacementPendingItem = newItem;

        // 아이템 교체 UI 띄우기
        itemReplacementUI.SetActive(true);

        // 항상 왼쪽 슬롯이 선택된 상태로 시작됨
        isSelectingSlot1ForReplacement = true;
    }
}
