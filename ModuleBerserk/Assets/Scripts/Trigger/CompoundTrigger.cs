using System.Collections.Generic;
using UnityEngine;

// 트리거 여럿이 모두 충족되어야 활성화되는 복합 트리거.
// ex) 양 옆의 레버를 당겨야만 열리는 문
//
// 복합 트리거는 직접 Activate() 또는 Deactivate()를 호출하지 않으며,
// 인스펙터에서 등록된 트리거 목록의 활성화 여부에 따라 자동으로 관리된다.
public class CompoundTrigger : Trigger
{
    [Header("Required Triggers")]
    // 한 개 이상의 트리거로 구성되는 목록.
    // 모든 트리거가 활성화되어야만 복합 트리거가 활성화된다.
    //
    // Note:
    // 목록이 비어있는 상황이라면 애초에 트리거를 쓸 필요가 없다는 뜻이므로
    // requiredTriggers.Count가 0경우는 고려하지 않았음.
    [SerializeField] private List<Trigger> requiredTriggers;

    // 활성화된 트리거의 수.
    // 각 트리거를 순회할 필요 없이 모든 조건이 충족되었는지 체크할 수 있게 해준다.
    private int numActiveTriggers = 0;

    private void Start()
    {
        foreach (var trigger in requiredTriggers)
        {
            trigger.OnActivate.AddListener(OnTriggerActivate);
            trigger.OnDeactivate.AddListener(OnTriggerDeactivate);
        }
    }

    private void OnDestroy()
    {
        foreach (var trigger in requiredTriggers)
        {
            trigger.OnActivate.RemoveListener(OnTriggerActivate);
            trigger.OnDeactivate.RemoveListener(OnTriggerDeactivate);
        }
    }

    private void OnTriggerActivate()
    {
        numActiveTriggers++;

        // 이번 트리거를 기점으로 모든 트리거가 활성화되었다면 복합 트리거 활성화
        if (numActiveTriggers == requiredTriggers.Count)
        {
            Activate();
        }
    }

    private void OnTriggerDeactivate()
    {
        // 모든 트리거가 활성화된 상태에서 하나라도 비활성화된 경우 복합 트리거 비활성화
        if (IsActive)
        {
            Deactivate();
        }

        numActiveTriggers--;
    }
}
