using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

// 여러 UI가 동시에 존재할 때 제일 위에 있는 (i.e. 가장 나중에 나온)
// UI만 입력을 처리할 수 있도록 관리하는 클래스.
//
// UI 입력을 처리하는 클래스들은 자신이 생성/활성화될 때 PushUserInterface(this)를,
// 자신이 삭제/비활성화될 때 PopUserInterface(this)를 각각 호출해야 함!
public class UserInterfaceStack
{
    private static List<IUserInterfaceController> userInterfaceControllers = new();

    public static void PushUserInterface(IUserInterfaceController controller)
    {
        // 이미 활성화된 UI가 있었다면 잠시 비활성화
        if (userInterfaceControllers.Count > 0)
        {
            userInterfaceControllers.Last().UnbindInputActions();
        }
        // 처음으로 뜬 UI라면 이 시점부터 UI 입력 처리 시작
        else
        {
            InputManager.InputActions.UI.Enable();
        }

        // 새로 생긴 UI를 스택에 넣고 입력 활성화
        controller.BindInputActions();
        userInterfaceControllers.Add(controller);
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
        }
        // 모든 UI가 사라졌다면 UI 입력 처리 중지
        else
        {
            InputManager.InputActions.UI.Disable();
        }
    }
}
