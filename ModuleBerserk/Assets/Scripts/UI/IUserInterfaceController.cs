// UI 입력을 받아 실제로 UI를 조작하는 클래스들이 구현해야 하는 인터페이스.
//
// UserInterfaceStack에서 최상단 UI만 입력을 처리할 수 있도록
// BindInputActions()와 UnbindInputActions()를 적절히 호출해준다.
public interface IUserInterfaceController
{
    void BindInputActions();
    void UnbindInputActions();
}
