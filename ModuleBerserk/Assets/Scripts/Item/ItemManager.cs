using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public struct ItemCooltimeUI
{
    public Slider slider;
    public Image iconImage;
    public Image fillImage;
}

// 필드에서 습득하는 아이템을 슬롯에 저장하고 사용하는 일을 담당하는 클래스
public class ItemManager : MonoBehaviour, IUserInterfaceController
{
    [Header("Item DB")]
    [SerializeField] private ActiveItemDatabase itemDatabase;


    [Header("Item Replacement")]
    [SerializeField] private GameObject itemReplacementUI;
    [SerializeField] private GameObject itemReplacementSlot1Outline;
    [SerializeField] private GameObject itemReplacementSlot2Outline;


    [Header("Cooltime UI")]
    [SerializeField] private ItemCooltimeUI slot1CooltimeUI;
    [SerializeField] private ItemCooltimeUI slot2CooltimeUI;


    // 아이템 교체 UI에서 현재 어느 슬롯을 교체 대상으로 지정하고 있는지
    private bool isSelectingSlot1ForReplacement = true;
    // 새로 습득해서 교체하려는 아이템
    private IActiveItem replacementPendingItem = null;

    // 각 슬롯에 들어있는 아이템.
    // 아이템 객체 자체는 직렬화가 까다로워서 맵에 진입할 때마다
    // ActiveItemDatabase에서 레퍼런스를 가져오는 방식으로 처리한다.
    private IActiveItem slot1Item = null;
    private IActiveItem slot2Item = null;

    // 게임 세션동안 유지되는 쿨타임 등의 상태
    private ItemSlotState slot1State = null;
    private ItemSlotState slot2State = null;

    // scene 로딩이 끝난 뒤 PlayerManager에 의해 호출되는 함수.
    // 직전 scene에서의 상태를 복원한다.
    public void InitializeState(ItemSlotState slot1State, ItemSlotState slot2State)
    {
        this.slot1State = slot1State;
        this.slot2State = slot2State;

        slot1Item = itemDatabase.GetItemInstance(slot1State.ItemType);
        slot2Item = itemDatabase.GetItemInstance(slot2State.ItemType);

        SetCooltimeUISprite(slot1Item, slot1CooltimeUI);
        SetCooltimeUISprite(slot2Item, slot2CooltimeUI);
    }

    public bool TryUseSlot1Item()
    {
        return TryUseItem(slot1Item, slot1State);
    }

    public bool TryUseSlot2Item()
    {
        return TryUseItem(slot2Item, slot2State);
    }

    private bool TryUseItem(IActiveItem item, ItemSlotState slotState)
    {
        if (item != null && slotState.UsageStack > 0)
        {
            item.Use();

            slotState.UsageStack--;

            return true;
        }

        return false;
    }

    private void Update()
    {
        HandleItemCooltime(slot1Item, slot1State);
        HandleItemCooltime(slot2Item, slot2State);

        UpdateCooltimeUI(slot1Item, slot1State, slot1CooltimeUI);
        UpdateCooltimeUI(slot2Item, slot2State, slot2CooltimeUI);
    }

    private void HandleItemCooltime(IActiveItem item, ItemSlotState slotState)
    {
        // 슬롯이 비어있으면 처리할 쿨타임도 없음
        if (item == null)
        {
            return;
        }

        if (slotState.UsageStack < item.GetMaxStack())
        {
            // case 1) 이미 쿨타임이 돌고 있는 경우
            if (slotState.Cooltime > 0)
            {
                slotState.Cooltime -= Time.deltaTime;

                // 쿨타임 끝났으면 사용 가능 횟수 증가
                if (slotState.Cooltime <= 0f)
                {
                    slotState.UsageStack++;
                }
            }
            // case 2) 새로 쿨타임을 시작하는 경우
            else
            {
                slotState.Cooltime = item.GetCooltime();
            }
        }
    }

    private void UpdateCooltimeUI(IActiveItem item, ItemSlotState slotState, ItemCooltimeUI cooltimeUI)
    {
        // case 1) 아이템 슬롯이 비어있는 경우 => 검은색 쿨타임 표시 없이 배경만 나와야 함
        if (item == null)
        {
            cooltimeUI.slider.value = 0f;
        }
        // case 2) 아이템이 있는 경우 => 쿨타임 표시
        else
        {
            float cooltimeRatio = slotState.Cooltime / item.GetCooltime();
            cooltimeUI.slider.value = cooltimeRatio;
        }
    }

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
            slot1Item = replacementPendingItem;
            HandleItemSlotChange(slot1Item, slot1State, slot1CooltimeUI);
        }
        else
        {
            slot2Item = replacementPendingItem;
            HandleItemSlotChange(slot2Item, slot2State, slot2CooltimeUI);
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
        if (slot1Item == null)
        {
            slot1Item = item;
            HandleItemSlotChange(slot1Item, slot1State, slot1CooltimeUI);
        }
        else if (slot2Item == null)
        {
            slot2Item = item;
            HandleItemSlotChange(slot2Item, slot2State, slot2CooltimeUI);
        }
        else
        {
            ShowItemReplacementUI(item);
        }
    }

    // 새로운 아이템을 습득했을 때 쿨타임 등의 슬롯 상태를
    // 갱신하고 쿨타임 UI의 아이콘을 변경함
    private void HandleItemSlotChange(IActiveItem item, ItemSlotState slotState, ItemCooltimeUI cooltimeUI)
    {
        slotState.ItemType = item.GetType();
        slotState.UsageStack = item.GetMaxStack();
        slotState.Cooltime = 0f;

        SetCooltimeUISprite(item, cooltimeUI);
    }

    private static void SetCooltimeUISprite(IActiveItem item, ItemCooltimeUI cooltimeUI)
    {
        // case 1) 아이템 슬롯이 비어있는 경우
        if (item == null)
        {
            cooltimeUI.iconImage.enabled = false;
        }
        // case 2) 슬롯에 뭔가 들어있는 경우
        else
        {
            cooltimeUI.iconImage.enabled = true;
            cooltimeUI.iconImage.sprite = item.GetItemSlotImage();
            cooltimeUI.fillImage.sprite = item.GetItemSlotImage();
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
