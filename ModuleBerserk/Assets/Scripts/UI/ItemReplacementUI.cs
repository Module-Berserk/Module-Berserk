using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemReplacementUI : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private Button slot1Button;
    [SerializeField] private Button slot2Button;
    [SerializeField] private Image slot1ItemIcon;
    [SerializeField] private Image slot2ItemIcon;

    public UnityEvent<int> OnSlotSelect = new(); // 왼쪽 슬롯을 선택하면 0, 오른쪽 슬롯을 선택하면 1을 전달함

    private void Awake()
    {
        slot1Button.onClick.AddListener(() => OnSlotSelect.Invoke(0));
        slot2Button.onClick.AddListener(() => OnSlotSelect.Invoke(1));
    }

    private void OnEnable()
    {
        UserInterfaceStack.PushUserInterface(this);
    }

    private void OnDisable()
    {
        UserInterfaceStack.PopUserInterface(this);
    }

    void IUserInterfaceController.BindInputActions()
    {
        slot1Button.interactable = true;
        slot2Button.interactable = true;

        var eventSystem = EventSystem.current;
        eventSystem.enabled = false;
        eventSystem.SetSelectedGameObject(slot1Button.gameObject);
        eventSystem.enabled = true;
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        slot1Button.interactable = false;
        slot2Button.interactable = false;
    }

    public void SetItemIcons(Sprite slot1Icon, Sprite slot2Icon)
    {
        slot1ItemIcon.sprite = slot1Icon;
        slot2ItemIcon.sprite = slot2Icon;
    }
}
