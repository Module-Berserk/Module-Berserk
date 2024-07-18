using System;

[Serializable]
public class ItemSlotState
{
    public ItemType ItemType {get; set;}
    public float Cooltime {get; set;}
    public int UsageStack {get; set;} // 잔여 사용 횟수

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
