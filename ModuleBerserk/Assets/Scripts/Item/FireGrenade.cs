using UnityEngine;

public class FireGrenade : MonoBehaviour, IActiveItem
{
    [SerializeField] private Sprite itemSlotImage;

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
        return 5f;
    }

    float IActiveItem.GetEffectDuration()
    {
        return 0f;
    }

    Sprite IActiveItem.GetItemSlotImage()
    {
        return itemSlotImage;
    }

    void IActiveItem.Use()
    {
        Debug.Log("화염 수류탄 사용");
    }
}
