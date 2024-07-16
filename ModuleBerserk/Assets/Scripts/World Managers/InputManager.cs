public class InputManager
{
    private static ModuleBerserkActionAssets inputActions;
    public static ModuleBerserkActionAssets InputActions
    {
        get
        {
            if (inputActions == null)
            {
                inputActions = new();

                // inputActions.UI는 UserInterfaceStack에서
                // UI 하나라도 생기면 자동으로 Enable() 해줘서 여기서 할 필요 x
                //
                // TODO: 완성 단계에서는 메인 메뉴에서 시작될 것이므로
                // Player 입력은 비활성화 상태로 시작하는게 좋을 것 같음.
                // 지금은 일단 미션 시작했다고 치고 Player 입력도 활성화.
                inputActions.Player.Enable();
                inputActions.Common.Enable();
            }

            return inputActions;
        }
    }
}
