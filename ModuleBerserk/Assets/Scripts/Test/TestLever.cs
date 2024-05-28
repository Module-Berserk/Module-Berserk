using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// Trigger와 조건부 상호작용을 테스트하기 위해 만든 클래스.
// 주변에 적이 없는 경우에만 상호작용을 할 수 있다.
//
// TODO: 정식 레버로 사용할 계획이라면 OnActivate와 OnDeactivate에 레버 애니메이션 추가할 것
public class TestLever : Trigger, IInteractable
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
        // 한 번 당기면 1초 뒤에 원상태로 돌아옴
        ActivateForDurationAsync(1f).Forget();
    }

    private async UniTask ActivateForDurationAsync(float duration)
    {
        Activate();
        await UniTask.WaitForSeconds(duration);
        Deactivate();
    }

    bool IInteractable.IsInteractionPossible()
    {
        // 조건이 충족되었고 레버가 이미 당겨진 상황이 아니라면 ok
        //
        // Note:
        // 엘리베이터에 사용되는 레버는 당겨도 잠시 뒤에 원상태로 돌아옴.
        // 플레이어가 작동시켜놓고 엘베 놓치는 경우 다시 작동시킬 수 있도록 하기 위함.
        return noEnemyNearbyTrigger.IsActive && !IsActive;
    }
}
