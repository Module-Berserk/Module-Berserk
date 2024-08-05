using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

// 여러 UI가 동시에 존재할 때 제일 위에 있는 (i.e. 가장 나중에 나온)
// UI만 입력을 처리할 수 있도록 관리하는 클래스.
//
// UI 입력을 처리하는 클래스들은 자신이 생성/활성화될 때 PushUserInterface(this)를,
// 자신이 삭제/비활성화될 때 PopUserInterface(this)를 각각 호출해야 함!
public class UserInterfaceStack
{
    private static List<IUserInterfaceController> userInterfaceControllers = new();
    private static List<GameObject> selectedUIElementStack = new();

    public static void PushUserInterface(IUserInterfaceController controller, GameObject firstSelectedUIElement)
    {
        // 이미 활성화된 UI가 있었다면 현재 어떤 UI 요소가 선택되었는지 기록해두고 잠시 비활성화
        if (userInterfaceControllers.Count > 0)
        {
            selectedUIElementStack.Add(EventSystem.current.currentSelectedGameObject);
            userInterfaceControllers.Last().UnbindInputActions();
        }
        // 처음으로 뜬 UI라면 이 시점부터 UI 입력 처리 시작
        else
        {
            InputManager.InputActions.UI.Enable();
        }

        // 새로 생긴 UI를 스택에 넣고 활성화
        controller.BindInputActions();
        userInterfaceControllers.Add(controller);

        // 처음으로 UI가 뜰 때 기본으로 선택되는 요소 설정하기 (ex. 첫 번째 버튼)
        //
        // 상호작용 키랑 UI 선택 키가 같다보니 상호작용으로 UI 창이 뜨는 동시에
        // 버튼이 바로 선택되어버리는 중복 입력 처리가 일어날 수 있기 때문에
        // EventSystem을 잠시 비활성화한 상태에서 처리해줘야 한다.
        var eventSystem = EventSystem.current;
        eventSystem.enabled = false;
        eventSystem.SetSelectedGameObject(firstSelectedUIElement);
        eventSystem.enabled = true;
    }

    public static void PopUserInterface(IUserInterfaceController controller)
    {
        // Pop은 UI 스택 최상단 요소만 호출할 수 있어야 정상
        Assert.IsTrue(ReferenceEquals(controller, userInterfaceControllers.Last()));

        // 기존 UI는 비활성화하고
        controller.UnbindInputActions();
        userInterfaceControllers.Remove(controller);

        // 아직 스택에 UI가 남아있다면 최상단 UI 다시 활성화
        if (userInterfaceControllers.Count > 0)
        {
            userInterfaceControllers.Last().BindInputActions();

            // 비활성화되기 직전의 UI navigation 상태를 복원
            GameObject previouslySelectedUIElement = selectedUIElementStack.Last();
            EventSystem.current.SetSelectedGameObject(previouslySelectedUIElement);
            selectedUIElementStack.Remove(previouslySelectedUIElement);
        }
        // 모든 UI가 사라졌다면 UI 입력 처리 중지
        else
        {
            InputManager.InputActions.UI.Disable();
        }
    }
}
