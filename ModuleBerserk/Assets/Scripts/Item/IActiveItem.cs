using UnityEngine;

public enum ItemCategory
{
    Grenade, // 투척형
    Bullet, // 직사형
    Turret, // 설치형
    ImmediateEffect, // 발동형
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Legendary,
}

public enum ItemType
{
    None, // 빈 아이템 슬롯을 표현하기 위한 값
    FireGrenade,
    SmokeGrenade,
    ShokeGrenade,
    PopPop, // 콩알탄
    FuelAdditive, // 연료 첨가제
    MiniTurret,
}

public interface IActiveItem
{
    // 아이템 식별용 정보
    ItemCategory GetCategory();
    ItemRarity GetRarity();
    ItemType GetType();

    string GetName();

    // 아이템의 효과가 끝난 이후 재사용까지 기다려야 하는 시간.
    // 설치형 아이템 등은 지속시간이 끝난 뒤에야 쿨타임이 돌기 시작한다!
    float GetCooltime();

    // 사용 횟수 스택이 존재해서 쿨타임이 돌면 스택이 1 추가되는 형식인 경우 override.
    // 보통 사용한 즉시 쿨타임이 돌기 때문에 1스택을 기본 값으로 넣어놨음.
    int GetMaxStack()
    {
        return 1;
    }

    // 지속형 또는 설치형 아이템의 유지 시간.
    // 이 시간이 지나야 쿨타임이 다시 돌기 시작한다.
    // 수류탄처럼 그냥 던지고 끝나는 아이템들은 0을 반환하면 된다.
    float GetEffectDuration();

    // 아이템 슬롯에 표시할 이미지.
    Sprite GetItemSlotImage();

    // 실제로 이 아이템을 사용하고 싶을 때 호출되는 함수.
    void Use();
}
