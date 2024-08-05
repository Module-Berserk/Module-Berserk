using UnityEngine;

public class Coal : MonoBehaviour, IInteractable
{
    // 플레이어가 먹으면 체력 얼마나 채워줄지
    [SerializeField] private float hpRegen;
    // 플레이어가 상호작용 범위에 들어오면 보여줄 텍스트
    [SerializeField] private GameObject interactionText;

    // 획득 애니메이션 재생하는 도중에 상호작용 막는 용도
    private bool isCollectible = true;

    void IInteractable.OnPlayerEnter()
    {
        if (isCollectible)
        {
            interactionText.SetActive(true);
        }
    }

    void IInteractable.OnPlayerExit()
    {
        interactionText.SetActive(false);
    }

    void IInteractable.StartInteraction()
    {
        if (isCollectible)
        {
            isCollectible = false;
            interactionText.SetActive(false); // 설명 숨기기

            GetComponent<Animator>().SetTrigger("Collect");
        }
    }

    // 애니메이션 마지막 프레임에 이벤트로 호출됨.
    // 체력 회복 & 오브젝트 삭제.
    private void OnCollectAnimationEnd()
    {
        GameStateManager.ActiveGameState.PlayerState.HP.ModifyBaseValue(hpRegen);
        Destroy(gameObject);
    }
}
