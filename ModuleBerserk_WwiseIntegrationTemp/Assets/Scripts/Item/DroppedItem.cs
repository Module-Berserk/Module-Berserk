using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject itemDescriptionUI;

    // TODO: 아이템 이름 대신 사용 가능한 IActiveItem 객체로 변경
    [SerializeField] private string itemName;

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
        var player = GameObject.FindGameObjectWithTag("Player");
        var itemManager = player.GetComponent<ItemManager>();

        // TODO: 아이템 이름 대신 사용 가능한 IActiveItem 객체로 변경
        itemManager.HandleItemCollect(itemName);

        Destroy(gameObject);
    }
}
