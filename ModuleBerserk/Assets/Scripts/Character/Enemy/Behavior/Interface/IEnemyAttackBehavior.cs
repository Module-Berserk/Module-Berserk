public interface IEnemyAttackBehavior
{
    bool IsAttackPossible();
    void StartAttack(CharacterStat baseDamage);
    void StopAttack();

    bool IsAttackMotionFinished {get; set;}
}
