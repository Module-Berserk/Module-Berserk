using UnityEngine;
using UnityEngine.Events;

// 조건 개념에 해당하는 모든 것들의 base class.
//
// 조건이 활성화된 순간을 OnActivate 이벤트로 알아내거나
// IsActive 프로퍼티로 현재 조건이 활성화되었는지 직접 확인할 수 있음.
// 
// 이 클래스를 상속받아서 원하는 시점에 Activate() 또는 Deactivate()를 호출해주면 됨.
// ex) 상호작용으로 레버를 당기면 문이 열리는 매커니즘 구현 방법
//     1. Trigger와 IInteractable을 상속하는 레버 클래스 생성
//     2. 레버의 StartInteraction()에서 Activate() 호출
//     3. 레버의 OnActivate 이벤트에 문 여는 함수 등록
public class Trigger : MonoBehaviour
{
    [Tooltip("Optional callback functions which are called when the trigger is activated")]
    public UnityEvent OnActivate;

    [Tooltip("Optional callback functions which are called when the trigger is deactivated")]
    public UnityEvent OnDeactivate;

    public bool IsActive {get; private set;} = false;

    public void Activate()
    {
        IsActive = true;
        OnActivate.Invoke();
    }

    public void Deactivate()
    {
        IsActive = false;
        OnDeactivate.Invoke();
    }
}
