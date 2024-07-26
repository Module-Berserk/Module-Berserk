using System;

[Serializable]
public class ItemSlotState
{
    public ItemType ItemType;
    public float Cooltime;
    public int UsageStack; // 잔여 사용 횟수


    // 새 게임을 시작할 때 사용할 초기 아이템 슬롯 상태를 준비함
    public ItemSlotState()
    {
        Reset();
    }

    public static ItemSlotState CreateDummyState()
    {
        return new ItemSlotState();
    }

    public void Reset()
    {
        ItemType = ItemType.None;
        Cooltime = 0f;
        UsageStack = 0;
    }
}
