using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ActiveItemBase : MonoBehaviour, IActiveItem
{
    [Header("Item Info")]
    [SerializeField] private ItemCategory category;
    [SerializeField] private ItemRarity rarity;
    [SerializeField] private ItemType type;
    [SerializeField] private string itemName;
    [SerializeField] private float cooltime;


    [Header("Images")]
    [SerializeField] private Sprite itemSlotImage;
    [SerializeField] private Sprite droppedItemImage;


    ItemCategory IActiveItem.GetCategory()
    {
        return category;
    }

    ItemRarity IActiveItem.GetRarity()
    {
        return rarity;
    }

    ItemType IActiveItem.GetType()
    {
        return type;
    }

    string IActiveItem.GetName()
    {
        return itemName;
    }

    float IActiveItem.GetCooltime()
    {
        return cooltime;
    }

    Sprite IActiveItem.GetItemSlotImage()
    {
        return itemSlotImage;
    }

    Sprite IActiveItem.GetDroppedItemImage()
    {
        return droppedItemImage;
    }

    public abstract void Use();
}
