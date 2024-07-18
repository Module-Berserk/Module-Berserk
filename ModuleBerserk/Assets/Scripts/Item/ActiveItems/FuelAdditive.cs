using Cysharp.Threading.Tasks;
using UnityEngine;

public class FuelAdditive : MonoBehaviour, IActiveItem
{
    [SerializeField] private Sprite itemSlotImage;
    [SerializeField] private Sprite droppedItemImage;

    ItemCategory IActiveItem.GetCategory()
    {
        return ItemCategory.ImmediateEffect;
    }

    ItemRarity IActiveItem.GetRarity()
    {
        return ItemRarity.Rare;
    }

    ItemType IActiveItem.GetType()
    {
        return ItemType.FuelAdditive;
    }

    string IActiveItem.GetName()
    {
        return "연료첨가제";
    }

    float IActiveItem.GetCooltime()
    {
        return 90f;
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
        GameStateManager
            .ActiveGameState
            .PlayerState
            .GearSystemState
            .GearGaugeGainCoefficient
            .ApplyMultiplicativeModifierForDurationAsync(1.5f, 60f).Forget();
    }
}
