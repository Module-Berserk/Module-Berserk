using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 플레이어와 상호작용 범위에 들어온 IInteractable 객체들을 관리하는 클래스.
// 작동하려면 Collider2D가 필요함.
public class InteractionManager : MonoBehaviour
{
    // 상호작용 범위에 들어온 IInteractable 목록 (ex. NPC, 드랍 아이템, ...)
    private List<IInteractable> availableInteractables = new();

    // 상호작용이 가능한 IInteractable 중에서 제일
    // 늦게 접촉한 대상의 StartInteraction()을 호출한다.
    //
    // 상호작용에 하나라도 성공하면 즉시 true를 반환하며,
    // 만약 범위 내에 아무도 없거나 모두 상호작용이
    // 불가능한 상태였다면 false를 반환한다.
    public bool TryStartInteractionWithLatestTarget()
    {
        for (int i = availableInteractables.Count - 1; i >= 0; --i)
        {
            if (availableInteractables[i].IsInteractionPossible())
            {
                availableInteractables[i].StartInteraction();
                return true;
            }
        }

        return false;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.TryGetComponent(out IInteractable interactable))
        {
            interactable.OnPlayerEnter();
            availableInteractables.Add(interactable);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.TryGetComponent(out IInteractable interactable))
        {
            interactable.OnPlayerExit();
            availableInteractables.Remove(interactable);
        }
    }
}
