using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 필드에서 습득하는 아이템을 슬롯에 저장하고 사용하는 일을 담당하는 클래스
public class ItemManager : MonoBehaviour
{
    public void HandleItemCollect(string item)
    {
        // TODO: 슬롯 두 개 만들고 빈 칸에 넣거나 기존 아이템과 교체하는 창 띄우기
        Debug.Log($"Collected item: {item}");
    }
}
