// 가까이 가서 E키로 상호작용할 수 있는 객체들의 인터페이스.
// Trigger로 설정된 Collider2D를 보유한 오브젝트에
// IInteractable의 자식 클래스가 컴포넌트로 부착된 경우 자동으로 처리됨.
public interface IInteractable
{
    // 플레이어가 상호작용 범위에 들어올 때 호출됨
    void OnPlayerEnter();

    // 플레이어가 상호작용 범위 밖으로 나갈 때 호출됨
    void OnPlayerExit();

    // 플레이어가 상호작용을 시도할 때 호출됨
    void StartInteraction();
}
