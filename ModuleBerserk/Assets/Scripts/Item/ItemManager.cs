using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 필드에서 습득하는 아이템을 슬롯에 저장하고 사용하는 일을 담당하는 클래스
public class ItemManager : MonoBehaviour
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

    // 아이템 교체 UI 활성화된 동안 입력을 막기 위해 필요함
    private PlayerManager playerManager;

    // TODO: ActionAssets 변수를 하나만 보유하고 여러 스크립트에서 참조하는 방안 고려
    private ModuleBerserkActionAssets actionAssets;
    // 아이템 교체 UI에서 현재 어느 슬롯을 교체 대상으로 지정하고 있는지
    private bool isSelectingSlot1ForReplacement = true;
    // 새로 습득해서 교체하려는 아이템
    // TODO: 타임을 string 대신 IActiveItem으로 변경
    private string replacementPendingItem = "";

    private void Awake()
    {
        FindComponentReferences();
        BindInputActions();
    }

    private void FindComponentReferences()
    {
        playerManager = GetComponent<PlayerManager>();
    }

    private void BindInputActions()
    {
        actionAssets = new ModuleBerserkActionAssets();

        actionAssets.UI.Select.performed += SelectItemReplacementSlot;
        actionAssets.UI.Left.performed += ChangeItemReplacementSlot;
        actionAssets.UI.Right.performed += ChangeItemReplacementSlot;
    }

    private void SelectItemReplacementSlot(InputAction.CallbackContext context)
    {
        if (itemReplacementUI.activeInHierarchy)
        {
            // UI 및 입력 비활성화 
            itemReplacementUI.SetActive(false);
            actionAssets.UI.Disable();

            // 플레이어 입력 다시 활성화
            playerManager.SetInputEnabled(true);

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
        // 교체 대상인 아이템 잠시 저장
        replacementPendingItem = newItem;

        // UI 및 입력 활성화
        itemReplacementUI.SetActive(true);
        actionAssets.UI.Enable();

        // 항상 왼쪽 슬롯이 선택된 상태로 시작됨
        isSelectingSlot1ForReplacement = true;

        // 플레이어 입력 잠시 비활성화
        playerManager.SetInputEnabled(false);
    }
}
