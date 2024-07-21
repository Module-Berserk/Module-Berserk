using System;

[Serializable]
public class ItemSlotState
{
    public ItemType ItemType;
    public float Cooltime;
    public int UsageStack; // 잔여 사용 횟수

    public static ItemSlotState CreateDummyState()
    {
        return new ItemSlotState
        {
            ItemType = ItemType.None,
            Cooltime = 0f,
            UsageStack = 0,
        };
    }
}
