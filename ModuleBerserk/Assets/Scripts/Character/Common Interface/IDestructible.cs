using UnityEngine;

// 플레이어의 공격이 플레이어를 자해하는 경우나
// 적의 공격이 또 다른 적을 공격하는 경우를 막기 위한 식별자.
// 공격을 시도한 주체의 Team이 자신의 Team과 다른 경우에만 데미지를 입힐 수 있다.
//
// note: IDestructible.ApplyDamage()
public enum Team
{
    Player,
    Enemy,
    Environment,
}

// 피격 대상이 얼마나 큰 경직을 얼마나 받아야 하는지 결정
public enum StaggerStrength
{
    None, // 경직 없음
    Weak, // 살짝 밀려나거나 제자리에서 경직 모션만 재생하는 수준
    Strong, // 크게 뒤로 밀려나며 넘어지는 수준
}

// 공격을 시도하는 주체부터 공격을 당하는 대상에게까지 흘러가는 정보들.
//
// 주의사항:
// damage는 TryApplyDamage()에 넘기는 시점에서는 raw damage이지만
// OnDamage 이벤트로 넘어가는 시점에서는 방어력을 고려한 최종 데미지로 수정됨.
public struct AttackInfo
{
    public Team damageSource; // 누가 데미지를 입혔는가? 적/플레이어/환경
    public float damage;
    public StaggerStrength staggerStrength; // 경직 강도
    public Vector2 knockbackForce; // 경직 부여에 성공하면 적용될 넉백 벡터
    public float duration; // 경직 지속 시간
}

// 체력과 방어력이 존재하며 파괴 가능한 모든 물체
// ex) 플레이어, 적, 필드에 배치된 나무 상자, ...
public interface IDestructible
{
    CharacterStat GetHPStat();
    CharacterStat GetDefenseStat();
    Team GetTeam();

    bool IsDestroyed { get => GetHPStat().CurrentValue <= 0f; }

    // 기본적으로는 무적 판정이 없음 (대부분의 잡몹)
    // 플레이어나 보스처럼 특별한 경우에만 이 함수를 override.
    bool IsInvincible()
    {
        return false;
    }

    // 공격을 받을 때마다 호출되며, HP 차감이나 경직 처리 등을 구현해야 함.
    // 챕터1 박스 기믹처럼 공격을 단순히 넉백 방향을 알아내기 위해 사용하는 경우도 존재하므로
    // 최종적으로 이 공격을 성공으로 봐야 할지 리턴 값으로 알려줘야 함.
    // 
    // HP 차감의 경우 플레이어처럼 긴급회피로 나중에 데미지를 무효화할 수 있는
    // 특수한 경우가 아니라면 그냥 HandleHPDecrease(finalDamage)를 호출하면 된다!
    bool OnDamage(AttackInfo attackInfo);

    // 공격을 받아 HP가 0이 된 경우 호출됨
    void OnDestruction();

    // 같은 팀의 공격이 아닌 경우 방어력을 고려한 데미지를 계산해 체력을 감소시킨다.
    // 만약 체력이 0에 도달한 경우 OnDestruction() 함수를 호출함.
    //
    // 무적 상태이거나 같은 팀이어서 데미지를 입히지 않은 경우 false를 반환하고,
    // 반대로 데미지를 성공적으로 입혔다면 true를 반환함.
    // 
    // note:
    // - 최초로 충돌한 대상에게만 데미지를 입히고 사라지는 총알의 경우 ApplyDamage()가
    //   true를 리턴하면 자신을 destroy하는 방식으로 리턴 값을 사용할 수 있다.
    // - Team에 의해 공격이 실패하는 경우로는 적이 쏜 총알이 앞에 있는 또 다른 적을 관통해
    //   플레이어에게 도달하는 경우를 생각할 수 있음.
    //
    // damageSource: 공격을 시도한 주체. 공격 대상과 같은 팀인 경우 무시됨.
    // rawDamage: 방어력을 고려하지 않은 데미지.
    // attackInfo: 이 공격이 부여할 경직의 강도와 방향. 경직이 없는 공격은 attackInfo.NoStagger를 넘겨주면 됨.
    bool TryApplyDamage(AttackInfo attackInfo)
    {
        // 이미 파괴된 물체는 공격에 반응하지 않음.
        // OnDestruction()이 여러 번 호출되는 것을 막아준다.
        if (IsDestroyed)
        {
            return false;
        }

        // 무적 판정이거나 같은 팀의 공격인 경우 무시함.
        if (IsInvincible() || attackInfo.damageSource == GetTeam())
        {
            return false;
        }

        // 넘겨받은 raw damage와 방어력을 기반으로 최종 데미지를 계산함.
        // 방어력 10을 기준으로 스탯 1마다 10%씩 최종 데미지가 차이남.
        CharacterStat def = GetDefenseStat();
        const float damageReductionPerDefense = 0.1f;
        float damageReduction = (def.CurrentValue - 10f) * damageReductionPerDefense;
        float finalDamage = attackInfo.damage * (1f - damageReduction);

        attackInfo.damage = finalDamage;

        // 데미지 처리 요청.
        // 대상이 이 공격이 성공이라고 판단하면 true를 반환할 것임.
        return OnDamage(attackInfo);
    }

    // HP 차감 및 사망 처리의 기본 구현.
    //
    // 플레이어가 긴급 회피로 데미지를 무효화할 가능성이 있어서
    // TryApplyDamage()에서 바로 처리하는 대신 OnDamage 이벤트에서
    // 각자 필요한 순간에 이 함수를 호출하는 방식으로 구현함.
    void HandleHPDecrease(float finalDamage)
    {
        // HP 스탯에는 버프/디버프가 없다고 가정.
        CharacterStat hp = GetHPStat();
        hp.ModifyBaseValue(-finalDamage);

        if (IsDestroyed)
        {
            OnDestruction();
        }
    }
}
