using UnityEngine;

public class FireGrenade : MonoBehaviour, IActiveItem
{
    [SerializeField] private Sprite itemSlotImage;
    [SerializeField] private Sprite droppedItemImage;

    ItemCategory IActiveItem.GetCategory()
    {
        return ItemCategory.Grenade;
    }

    ItemRarity IActiveItem.GetRarity()
    {
        return ItemRarity.Common;
    }

    ItemType IActiveItem.GetType()
    {
        return ItemType.FireGrenade;
    }

    string IActiveItem.GetName()
    {
        return "화염병";
    }

    float IActiveItem.GetCooltime()
    {
        return 1f;
    }

    Sprite IActiveItem.GetItemSlotImage()
    {
        return itemSlotImage;
    }

    Sprite IActiveItem.GetDroppedItemImage()
    {
        return droppedItemImage;
    }

    void IActiveItem.Use()
    {
        Debug.Log("화염 수류탄 사용");
        // TODO: 수류탄 투척
    }
}
