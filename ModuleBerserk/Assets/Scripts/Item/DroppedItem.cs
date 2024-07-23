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

        // 부드럽게 위아래로 움직이는 모션
        const float motionHeight = 0.3f;
        transform.DOMoveY(transform.position.y + motionHeight, duration: 1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDestroy()
    {
        // 드랍 아이템의 위아래 움직임은 무한지속이라 transform이 삭제될 때 같이 취소해줘야함
        transform.DOKill();
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
