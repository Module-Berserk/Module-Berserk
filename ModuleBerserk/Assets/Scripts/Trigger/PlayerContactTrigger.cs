using UnityEngine;

public class PlayerContactTrigger : Trigger
{
    // 컷신처럼 한번만 활성화되어야 하는 트리거인 경우 true로 설정
    [SerializeField] private bool isOneTimeTrigger = false;
    [SerializeField] private Color gizmoColor;

    private bool isAlreadyTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 일회성 트리거로 설정된 경우 최초의 충돌이 아니라면 처리 x
            if (isOneTimeTrigger && isAlreadyTriggered)
            {
                return;
            }

            Activate();
            isAlreadyTriggered = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Deactivate();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, GetComponent<BoxCollider2D>().size);
    }
}
