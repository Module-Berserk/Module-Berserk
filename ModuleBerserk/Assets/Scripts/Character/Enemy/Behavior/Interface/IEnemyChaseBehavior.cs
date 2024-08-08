public interface IEnemyChaseBehavior
{
    // 플레이어를 추적하는 동안 매 스텝 호출되는 함수
    void ChasePlayer();

    // 플레이어가 추적 가능한 범위에 존재하는지 반환
    bool CanChasePlayer();
    
    // 공격 쿨타임이 도는 중이라 기다리는 등 가만히 서있어야 할 때 매 스텝 호출됨
    void Idle();
}
