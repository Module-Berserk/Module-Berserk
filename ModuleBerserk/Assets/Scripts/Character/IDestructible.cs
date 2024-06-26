using System;
using System.Collections;
using System.Collections.Generic;
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

public struct StaggerInfo
{
    public StaggerStrength strength; // 경직 강도
    public Vector2 direction; // 밀려날 방향 (TODO: 만약 왼쪽/오른쪽만 필요하다면 Vector2에서 bool 또는 enum으로 변경)

    public StaggerInfo(StaggerStrength strength, Vector2 direction)
    {
        this.strength = strength;
        this.direction = direction;
    }

    // 경직 없이 데미지를 입히고 싶은 경우 사용
    public static StaggerInfo NoStagger => new(StaggerStrength.None, Vector2.zero);
}

// 체력과 방어력이 존재하며 파괴 가능한 모든 물체
// ex) 플레이어, 적, 필드에 배치된 나무 상자, ...
public interface IDestructible
{
    CharacterStat GetHPStat();
    CharacterStat GetDefenseStat();
    Team GetTeam();

    // 공격을 받을 때마다 호출됨
    //
    // TODO:
    // 만약 CharacterStat.OnValueChange 이벤트에서 파라미터로 알려주는 스탯 변화량을
    // 직접 stat.CurrentValue를 사용해 알아내거나 이 함수처럼 변화량을 다른 방법으로
    // 알아낼 수 있다고 한다면 CharacterStat.OnValueChange 이벤트 타입을
    // UnityEvent<float>에서 그냥 UnityEvent로 바꿔도 됨.
    // 생각해보니 스탯이 바뀔 때마다 현재 수치 말고 변화량 자체가 필요한 곳이 별로 없는 것 같음...
    void OnDamage(float finalDamage, StaggerInfo staggerInfo);

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
    // staggerInfo: 이 공격이 부여할 경직의 강도와 방향. 경직이 없는 공격은 StaggerInfo.NoStagger를 넘겨주면 됨.
    bool TryApplyDamage(Team damageSource, float rawDamage, StaggerInfo staggerInfo)
    {
        // 같은 팀의 공격인 경우 무시함.
        // TODO: 무적 상태인 경우에도 데미지를 무시하도록 수정
        if (damageSource == GetTeam())
        {
            return false;
        }

        CharacterStat hp = GetHPStat();
        CharacterStat def = GetDefenseStat();

        // 방어력 10을 기준으로 스탯 1마다 10%씩 최종 데미지가 차이남.
        const float damageReductionPerDefense = 0.1f;
        float damageReduction = (def.CurrentValue - 10f) * damageReductionPerDefense;

        // TODO: 슈퍼아머 상태라면 데미지 10% 더 받게 만들기
        float finalDamage = rawDamage * (1f - damageReduction);

        // HP 스탯에는 버프/디버프가 없다고 가정.
        hp.ModifyBaseValue(-finalDamage);


        // 데미지 및 파괴 이벤트
        OnDamage(finalDamage, staggerInfo);
        if (hp.CurrentValue <= 0f)
        {
            OnDestruction();
        }

        return true;
    }
}
