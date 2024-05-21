// EnemyBehavior 계열의 클래스는 적이 할 수 있는 행동들을 제공하며,
// 이 행동들을 수행하는 규칙은 EnemyController 계열의 클래스에서 정의함.
//
// 이름이 StartXXX인 함수들은 해당 상태가 시작될 때 단발성으로 호출되지만,
// Chase 같은 나머지 함수들은 해당 상태일 경우 매 프레임 호출됨.
public interface IEnemyBehavior
{
    // 대기 상태로 전환될 때 호출되는 함수
    void StartIdle();

    // 플레이어를 추적하는 동안 매 스텝 호출되는 함수
    void Chase();

    // 플레이어가 추적 가능한 범위에 존재하는지
    bool CanChasePlayer();

    // 플레이어가 추적 범위를 벗어나 원래 위치로 돌아오는 동안 매 스탭 호출되는 함수.
    // 이동이 끝나지 않았다면 false를, 끝났다면 true를 반환해야 함.
    bool ReturnToInitialPosition();

    // 순찰 상태에서 매 스텝 호출되는 함수.
    // 플랫폼에서 낙하하지 않는 선에서 주어진 방향으로 이동해야 함.
    // 정지하는 경우 speed = 0이 주어짐.
    void Patrol(float speed);

    // 피격 경직을 부여할 때 호출되는 함수.
    // 만약 슈퍼아머 상태라면 아무것도 안 하고 false를 리턴해야 함.
    // 반대로 경직이 적용되었다면 true를 리턴해야 함.
    bool TryApplyStagger(StaggerInfo staggerInfo);
}
