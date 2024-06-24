using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

public class DialogueBox : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private TextMeshPro textMesh;

    [SerializeField] private float widthPadding = 1f;
    [SerializeField] private float heightPadding = 1f;
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private float characterAppearanceDelay = 0.02f;

    private bool isSelectingDialogueOption = false;
    private int selectedDialogueOptionIndex;
    private List<string> dialogueOptions;

    private void Start()
    {
        TestDialogueSelection().Forget();
        // TestDialogueSequence().Forget();
    }

    private async UniTask TestDialogueSelection()
    {
        List<string> options = new();
        options.Add("1. 커피를 마신다");
        options.Add("2. 도넛을 먹는다");
        options.Add("3. 굶는다");

        int result = await BeginDialogueSelection(options);

        Debug.Log($"최종 선택: {options[result]}");
    }

    private async UniTask TestDialogueSequence()
    {
        await BeginTypingAnimation("하늘을 올려다봐도 하늘밖에 없다");
        await UniTask.WaitForSeconds(1f);
        await BeginTypingAnimation("강은 바다로 넓어지고 사람은 죽음으로 넘치네");
        await UniTask.WaitForSeconds(1f);
        await BeginTypingAnimation("추운 겨울에는 <u>따뜻한 커피</u>와 티를 마셔야지요");
        await UniTask.WaitForSeconds(1f);
        await BeginTypingAnimation("사막에서 걷는 법을 알려줘\n신발에 모래가 들어가서 발을 델 것 같아");
    }

    private async UniTask BeginTypingAnimation(string dialogue)
    {
        textMesh.text = dialogue;
        AdjustBackgroundSize();

        // 아무 글자도 보이지 않는 상태에서 한 글자씩 출력
        textMesh.maxVisibleCharacters = 0;

        while (textMesh.maxVisibleCharacters < textMesh.text.Length)
        {
            textMesh.maxVisibleCharacters += 1;

            await UniTask.WaitForSeconds(characterAppearanceDelay);
        }
    }

    // 전체 텍스트가 들어갈 수 있도록 배경 이미지의 사이즈를 조정
    private void AdjustBackgroundSize()
    {
        // 1. 전체 텍스트의 mesh를 준비한다
        textMesh.maxVisibleCharacters = textMesh.text.Length;
        textMesh.ForceMeshUpdate();

        // 2. mesh의 크기를 기준으로 약간의 padding을 더해 배경 이미지 크기를 조정한다
        backgroundRenderer.size = new Vector2(textMesh.renderedWidth + widthPadding, textMesh.renderedHeight + heightPadding);

        // 3. 텍스트와 배경 이미지 모두 중앙에 정렬되므로
        // 말풍선 꼬리가 제자리에 머물도록 약간 오른쪽으로 중심을 옮긴다
        transform.localPosition = backgroundRenderer.size / 2f + Vector2.up * heightOffset;
    }

    public async UniTask<int> BeginDialogueSelection(List<string> options)
    {
        // 첫 번째 옵션을 기본 선택으로 삼고 대화창 출력
        dialogueOptions = options;
        isSelectingDialogueOption = true;
        selectedDialogueOptionIndex = 0;
        UpdateDialogueOptionText();
        AdjustBackgroundSize();

        // 입력 활성화하고 선택이 끝날 때까지 대기
        UserInterfaceStack.PushUserInterface(this);
        await UniTask.WaitUntil(() => !isSelectingDialogueOption);
        UserInterfaceStack.PopUserInterface(this);

        // 최종적으로 선택한 번호 반환
        return selectedDialogueOptionIndex;
    }

    void IUserInterfaceController.BindInputActions()
    {
        InputManager.InputActions.UI.Up.performed += SelectUpperDialogueOption;
        InputManager.InputActions.UI.Down.performed += SelectLowerDialogueOption;
        InputManager.InputActions.UI.Select.performed += FinishSelectingDialogueOption;
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        InputManager.InputActions.UI.Up.performed -= SelectUpperDialogueOption;
        InputManager.InputActions.UI.Down.performed -= SelectLowerDialogueOption;
        InputManager.InputActions.UI.Select.performed -= FinishSelectingDialogueOption;
    }

    public void SelectUpperDialogueOption(InputAction.CallbackContext context)
    {
        selectedDialogueOptionIndex = Mathf.Max(0, selectedDialogueOptionIndex - 1);
        UpdateDialogueOptionText();
    }

    public void SelectLowerDialogueOption(InputAction.CallbackContext context)
    {
        selectedDialogueOptionIndex = Mathf.Min(dialogueOptions.Count - 1, selectedDialogueOptionIndex + 1);
        UpdateDialogueOptionText();
    }

    public void FinishSelectingDialogueOption(InputAction.CallbackContext context)
    {
        isSelectingDialogueOption = false;
    }

    private void UpdateDialogueOptionText()
    {
        string newText = "";
        for (int i = 0; i < dialogueOptions.Count; ++i)
        {
            // 현재 선택중인 옵션에만 밑줄 추가
            if (i == selectedDialogueOptionIndex)
            {
                newText += $"<u>{dialogueOptions[i]}</u>";
            }
            else
            {
                newText += $"{dialogueOptions[i]}";
            }

            // 마지막 옵션이 아닌 경우 개행
            if (i != dialogueOptions.Count - 1)
            {
                newText += "\n";
            }
        }

        textMesh.text = newText;
    }
}
