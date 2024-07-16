// 가까이 가서 E키로 상호작용할 수 있는 객체들의 인터페이스.
// Trigger로 설정된 Collider2D를 보유한 오브젝트에
// IInteractable의 자식 클래스가 컴포넌트로 부착된 경우 자동으로 처리됨.
public interface IInteractable
{
    // 플레이어가 상호작용 범위에 들어올 때 호출됨
    // 레버처럼 따로 UI 표시가 없는 경우도 있어서 빈 기본 구현체를 제공함
    void OnPlayerEnter() {}

    // 플레이어가 상호작용 범위 밖으로 나갈 때 호출됨
    // 레버처럼 따로 UI 표시가 없는 경우도 있어서 빈 기본 구현체를 제공함
    void OnPlayerExit() {}

    // 플레이어가 상호작용을 시도할 때 호출됨
    void StartInteraction();

    // 현재 상호작용이 가능한 상태인지 반환 (기본 설정은 항상 true)
    // 만약 특정 조건이 충족된 상황에서만 상호작용이 허용되어야 한다면
    // Trigger 계열의 컴포넌트를 활용해 아래와 같이 override하면 된다.
    //
    // ex) 주위에 적이 없는 경우에만 레버를 당길 수 있어야 함
    // bool IInteractable.IsInteractionPossible()
    // {
    //     // noEnemyTrigger는 주변에 적이 없을 때
    //     // 활성화되는 Trigger의 자식 클래스라고 가정
    //     return noEnemyTrigger.IsActive;
    // }
    bool IsInteractionPossible()
    {
        return true;
    }
}
