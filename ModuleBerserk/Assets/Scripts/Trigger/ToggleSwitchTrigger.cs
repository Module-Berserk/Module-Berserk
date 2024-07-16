// 레버처럼 On/Off 상태를 오고가는 상호작용 가능한 트리거들의 base class.
// 편의를 위해 토글 함수를 제공한다.
public abstract class ToggleSwitchTrigger : Trigger
{
    protected void Toggle()
    {
        if (IsActive)
        {
            Deactivate();
        }
        else
        {
            Activate();
        }
    }
}
