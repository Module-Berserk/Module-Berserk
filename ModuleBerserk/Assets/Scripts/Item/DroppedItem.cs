using DG.Tweening;
using TMPro;
using UnityEngine;

public class DroppedItem : MonoBehaviour, IInteractable
{
    [SerializeField] private TextMeshProUGUI itemName;

    [Header("Rarity Color")]
    [SerializeField, ColorUsageAttribute(true,true)] private Color commonItemGlowColor;
    [SerializeField, ColorUsageAttribute(true,true)] private Color rareItemGlowColor;
    [SerializeField, ColorUsageAttribute(true,true)] private Color legendaryItemGlowColor;

    public GameObject ItemPrefab;

    private void Start()
    {
        var item = ItemPrefab.GetComponent<IActiveItem>();

        // 아이템 아이콘 설정
        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = item.GetDroppedItemImage();

        // 레어도에 따른 테두리 색 변경
        var glowColor = FindRarityGlowColor(item.GetRarity());
        spriteRenderer.material.SetColor("_GlowColor", glowColor);

        // 가까이 가면 뜨는 아이템 이름 설정
        itemName.text = item.GetName();
    }

    private Color FindRarityGlowColor(ItemRarity rarity)
    {
        if (rarity == ItemRarity.Common)
        {
            return commonItemGlowColor;
        }
        else if (rarity == ItemRarity.Rare)
        {
            return rareItemGlowColor;
        }
        else
        {
            return legendaryItemGlowColor;
        }
    }

    public void OnPlayerEnter()
    {
        itemName.enabled = true;
    }

    public void OnPlayerExit()
    {
        itemName.enabled = false;
    }

    public void StartInteraction()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var itemManager = player.GetComponent<ItemManager>();

        itemManager.HandleItemCollect(ItemPrefab.GetComponent<IActiveItem>());

        transform.DOKill();

        Destroy(gameObject);
    }
}
