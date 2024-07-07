using Cysharp.Threading.Tasks;
using UnityEngine;

// 챕터1 맵의 엘리베이터를 조작하기 위한 레버 컨트롤러.
// Trigger를 준 경우 주변에 적이 없는 경우에만 상호작용을 할 수 있다.
public class C1ElevatorLever : Trigger, IInteractable
{
    [Header("Required Trigger")]
    // 레버 조작에 조건을 부여하고 싶은 경우 사용
    // ex) 주변에 적이 없을 때만 가능 => NoEnemyNearbyTrigger 레퍼런스 넣기
    [SerializeField] private Trigger requiredTrigger;

    [Header("Activation Sprites")]
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite inactiveSprite;

    [Header("Reactivate")]
    // 레버를 당긴 뒤 다시 원상태로 돌아오기까지 걸리는 시간.
    // 엘리베이터의 이동 시간에 맞춰서 설정해줘야 자연스럽다.
    [SerializeField] private float timeToReactivate;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void IInteractable.StartInteraction()
    {
        // 당기면 잠깐 기다렸다가 원상태로 돌아옴
        ActivateForDurationAsync(timeToReactivate).Forget();
    }

    private async UniTask ActivateForDurationAsync(float duration)
    {
        Activate();
        spriteRenderer.sprite = activeSprite;
        await UniTask.WaitForSeconds(duration);
        spriteRenderer.sprite = inactiveSprite;
        Deactivate();
    }

    bool IInteractable.IsInteractionPossible()
    {
        // 조건이 충족되었고 레버가 이미 당겨진 상황이 아니라면 ok.
        // 조건이 없는 경우는 항상 조건이 충족된 것으로 취급함.
        //
        // Note:
        // 엘리베이터에 사용되는 레버는 당겨도 잠시 뒤에 원상태로 돌아옴.
        // 플레이어가 작동시켜놓고 엘베 놓치는 경우 다시 작동시킬 수 있도록 하기 위함.
        return (requiredTrigger == null || requiredTrigger.IsActive) && !IsActive;
    }
}
