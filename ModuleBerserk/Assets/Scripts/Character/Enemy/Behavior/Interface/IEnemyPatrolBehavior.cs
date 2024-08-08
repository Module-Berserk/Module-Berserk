public interface IEnemyPatrolBehavior
{
    // 순찰 상태에 진입할 때 호출되는 함수
    void StartPatrol();

    // 순찰 도중 플레이어를 발견해서 추적 상태로 전환될 때 호출되는 함수
    void StopPatrol();
}
