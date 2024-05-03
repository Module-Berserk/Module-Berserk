using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour, IInteractable
{
    public void OnPlayerEnter()
    {
        // TODO: 아이템 정보 띄우기
        Debug.Log("Player entered interaction range");
    }

    public void OnPlayerExit()
    {
        // TODO: 아이템 정보 감추기
        Debug.Log("Player exited interaction range");
    }

    public void StartInteraction()
    {
        // TODO: 아이템 슬롯이 비어있다면 바로 장착하고, 아니라면 아이템 교체 UI 띄우기
        Debug.Log("Starting interaction");
    }
}
