using System;

[Serializable]
public class ItemSlotState
{
    ItemType ItemType {get; set;}
    float Cooltime {get; set;}
    int UsageStack {get; set;} // 잔여 사용 횟수

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
