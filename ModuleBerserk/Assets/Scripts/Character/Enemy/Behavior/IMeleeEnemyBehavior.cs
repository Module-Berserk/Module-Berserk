// 근접 공격을 하는 잡몹의 행동 목록을 정의하는 인터페이스.
// 대기, 추격, 그리고 근접 공격을 정의해야 한다.
public interface IMeleeEnemyBehavior : IEnemyBehavior
{
    // 근접 공격을 트리거하는 함수
    void MeleeAttack();

    // 공격 쿨타임 등을 고려해 근접 공격이 준비된 시점에만 true를 반환
    bool IsMeleeAttackReady();
}
