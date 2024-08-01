using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 예/아니오처럼 두 가지 선택지를 제공하는 버튼 UI.
// 둘 중 하나를 고르면 이벤트로 결과를 알려주고 자동으로 비활성화된다.
//
// ex) 데이터가 존재하는 세이브 슬롯에 덮어쓰기 하려는 경우 뜨는 경고창
public class YesNoSelectionUI : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    // yes를 누르면 true, no를 누르면 false를 전달함.
    // 선택이 끝나면 자동으로 모든 등록된 콜백을 정리해준다.
    public UnityEvent<bool> OnSelect = new();

    private void Awake()
    {
        yesButton.onClick.AddListener(() => {
            OnSelect.Invoke(true); // yes를 눌렀다고 알려줌
            DisableSelf(); // 선택 끝났으니 자동 비활성화
        });
        noButton.onClick.AddListener(() => {
            OnSelect.Invoke(false); // no를 눌렀다고 알려줌
            DisableSelf(); // 선택 끝났으니 자동 비활성화
        });
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false); 
        OnSelect.RemoveAllListeners();
    }

    private void OnEnable()
    {
        UserInterfaceStack.PushUserInterface(this, yesButton.gameObject);
    }

    private void OnDisable()
    {
        UserInterfaceStack.PopUserInterface(this);
    }

    void IUserInterfaceController.BindInputActions()
    {
        yesButton.interactable = true;
        noButton.interactable = true;
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        yesButton.interactable = false;
        noButton.interactable = false;
    }
}
