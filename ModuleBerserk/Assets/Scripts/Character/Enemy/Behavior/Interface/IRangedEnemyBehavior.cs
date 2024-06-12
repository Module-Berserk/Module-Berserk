// 원거리 공격을 하는 잡몹의 행동 목록을 정의하는 인터페이스.
// 대기, 추격, 그리고 원거리 공격, 밀쳐내기, 그리고 도주를 정의해야 한다.
public interface IRangedEnemyBehavior : IEnemyBehavior
{
    // 원거리 공격을 트리거하는 함수
    void RangedAttack();

    // 공격 쿨타임 등을 고려해 원거리 공격이 준비된 시점에만 true를 반환
    bool IsRangedAttackReady();

    // 플레이어가 너무 가까워지면 밀쳐내는 공격을 시도
    void RepelAttack();

    // 밀쳐내기를 다시 사용할 수 있는 시점에만 true를 반환
    bool IsRepelAttackReady();

    // 원거리 공격 또는 밀쳐내기 애니메이션이 재생 중이지 않은 경우 true를 반환.
    // 모션 도중에 추가적인 행동을 하지 못하도록 막기 위해 사용.
    bool IsAttackMotionFinished();

    // 최소 사정거리를 확보하기 위해 후퇴할 때 매 스텝 호출되는 함수
    void RunAway();
}
