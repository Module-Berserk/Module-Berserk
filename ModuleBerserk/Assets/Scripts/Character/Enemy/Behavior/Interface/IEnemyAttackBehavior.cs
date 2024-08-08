public interface IEnemyAttackBehavior
{
    bool IsAttackPossible();
    void StartAttack();
    void StopAttack();

    bool IsAttackMotionFinished {get; set;}
}
