public interface IEnemyChaseBehavior
{
    // 플레이어를 추적하는 동안 매 스텝 호출되는 함수
    void ChasePlayer();

    // 플레이어가 추적 가능한 범위에 존재하는지 반환
    bool CanChasePlayer();
}
