public interface IEnemyStaggerBehavior
{
    bool IsStaggered {get; set;}

    bool TryApplyStagger(AttackInfo attackInfo);
}
