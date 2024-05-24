// 레버처럼 On/Off 상태를 오고가는 상호작용 가능한 트리거들의 base class.
// 상속 받아서 OnPlayerEnter()와 OnPlayerExit()만 구현해주면 됨.
public abstract class ToggleSwitchTrigger : Trigger, IInteractable
{
    public abstract void OnPlayerEnter();
    public abstract void OnPlayerExit();

    public void StartInteraction()
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
