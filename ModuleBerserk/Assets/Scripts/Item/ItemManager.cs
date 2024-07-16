using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

// 필드에서 습득하는 아이템을 슬롯에 저장하고 사용하는 일을 담당하는 클래스
public class ItemManager : MonoBehaviour, IUserInterfaceController
{
    // 아직 UI 없으니까 인스펙터에서 값 확인하기 위해 임시로 public으로 설정함
    // TODO:
    // 1. 아이템 슬롯 UI 추가되면 private으로 변경
    // 2. 타입을 string 대신 IActiveItem으로 변경
    public string slot1 = "";
    public string slot2 = "";

    [SerializeField] private GameObject itemReplacementUI;
    [SerializeField] private GameObject itemReplacementSlot1Outline;
    [SerializeField] private GameObject itemReplacementSlot2Outline;

    // 아이템 교체 UI에서 현재 어느 슬롯을 교체 대상으로 지정하고 있는지
    private bool isSelectingSlot1ForReplacement = true;
    // 새로 습득해서 교체하려는 아이템
    // TODO: 타임을 string 대신 IActiveItem으로 변경
    private string replacementPendingItem = "";

    void IUserInterfaceController.BindInputActions()
    {
        // TODO: UI 스택의 최상단에 있는 메뉴만 UI 입력을 처리해야 함!
        // ex) 아이템 교체 UI 뜬 상태에서 일시정지 => 일시정지 메뉴만 조작 가능
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

        Debug.Log("아이템 교체 완료");

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

    public void HandleItemCollect(string item)
    {
        if (slot1 == "")
        {
            slot1 = item;
        }
        else if (slot2 == "")
        {
            slot2 = item;
        }
        else
        {
            ShowItemReplacementUI(item);
        }
    }

    private void ShowItemReplacementUI(string newItem)
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
