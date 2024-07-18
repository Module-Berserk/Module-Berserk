using DG.Tweening;
using TMPro;
using UnityEngine;

public class DroppedItem : MonoBehaviour, IInteractable
{
    [SerializeField] private TextMeshProUGUI itemName;

    public GameObject ItemPrefab;

    private void Start()
    {
        var item = ItemPrefab.GetComponent<IActiveItem>();

        // 아이템 아이콘 설정
        var spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = item.GetItemSlotImage();

        // 가까이 가면 뜨는 아이템 이름 설정
        itemName.text = item.GetName();

        // 부드럽게 위아래로 움직이는 모션
        const float motionHeight = 0.3f;
        transform.DOMoveY(transform.position.y + motionHeight, duration: 1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
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

        Destroy(gameObject);
    }
}
