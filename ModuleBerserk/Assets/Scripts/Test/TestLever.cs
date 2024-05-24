using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Trigger와 조건부 상호작용을 테스트하기 위해 만든 클래스.
// 주변에 적이 없는 경우에만 상호작용을 할 수 있다.
public class TestLever : ToggleSwitchTrigger, IInteractable
{
    [Header("Required Trigger")]
    [SerializeField] private NoEnemyNearbyTrigger noEnemyNearbyTrigger;

    void IInteractable.OnPlayerEnter()
    {
        Debug.Log("플레이어 접촉");
    }

    void IInteractable.OnPlayerExit()
    {
        Debug.Log("플레이어 멀어짐");
    }

    void IInteractable.StartInteraction()
    {
        Toggle();
    }

    bool IInteractable.IsInteractionPossible()
    {
        return noEnemyNearbyTrigger.IsActive;
    }
}
