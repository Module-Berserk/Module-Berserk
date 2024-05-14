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

    public bool CanInteract {get => availableInteractables.Count > 0;}

    // 제일 마지막으로 범위에 들어온 대상과 상호작용 시작
    public void StartInteractionWithLatestTarget()
    {
        availableInteractables.Last().StartInteraction();
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
