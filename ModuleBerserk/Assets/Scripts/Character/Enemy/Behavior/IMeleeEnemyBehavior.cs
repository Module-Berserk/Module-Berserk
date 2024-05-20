// 근접 공격을 하는 잡몹의 행동 목록을 정의하는 인터페이스.
// 대기, 추격, 그리고 근접 공격을 정의해야 한다.
public interface IMeleeEnemyBehavior : IEnemyBehavior
{
    void MeleeAttack();
}
