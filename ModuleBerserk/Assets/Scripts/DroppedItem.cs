using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject itemDescriptionUI;

    public void OnPlayerEnter()
    {
        itemDescriptionUI.SetActive(true);
    }

    public void OnPlayerExit()
    {
        itemDescriptionUI.SetActive(false);
    }

    public void StartInteraction()
    {
        // TODO: 아이템 슬롯이 비어있다면 바로 장착하고, 아니라면 아이템 교체 UI 띄우기
        Debug.Log("Starting interaction");
    }
}
