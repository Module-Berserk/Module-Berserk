using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using UnityEngine.Assertions;

// 등장인물 머리 위에 말풍선 모양으로 대사를 출력해주는 스크립트
public class DialogueBox : MonoBehaviour, IUserInterfaceController
{
    [Header("Background Sprite Variant")]
    [SerializeField] private Sprite leftBumpBackground;
    [SerializeField] private Sprite rightBumpBackground;
    [SerializeField] private Sprite noBumpBackground;


    [Header("Component References")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private TextMeshPro textMesh;

    
    [Header("Dialogue Print Options")]
    // 텍스트의 mesh보다 배경 이미지가 얼마나 커야 하는지 결정.
    // 배경 이미지는 테두리에 빈 공간이 있어서 텍스트의 크기보다
    // 약간 커야 말풍선 안에 대사가 예쁘게 들어간다.
    [SerializeField] private float widthPadding = 1f;
    [SerializeField] private float heightPadding = 1f;
    // 말풍선의 꼬리가 스프라이트의 pivot보다 얼마나 위에 있어야 하는지 결정.
    // 주인공은 pivot이 머리에 있어서 0으로 둬도 괜찮음.
    [SerializeField] private float heightOffset = 0f;
    // 대사를 한 글자씩 출력할 때 글자 하나마다 소요되는 시간
    [SerializeField] private float characterAppearanceDelay = 0.02f;

    // 말풍선 꼬리의 위치
    private enum BumpLocation
    {
        Left,
        Right,
        None,
    }
    [SerializeField] private BumpLocation bumpLocation = BumpLocation.Left;

    private bool isSelectingDialogueOption = false;
    private int selectedDialogueOptionIndex;
    private List<string> dialogueOptions;

    private void Start()
    {
        AssertConfiguration();
        ChooseBacgroundSpriteWithCorrectBump();
    }

    // 정상적인 대화창 prefab 설정인지 확인
    private void AssertConfiguration()
    {
        // 컴포넌트 레퍼런스 설정 완료
        Assert.IsNotNull(backgroundRenderer);
        Assert.IsNotNull(textMesh);

        // 텍스트도 배경 이미지와 마찬가지로 중앙에 정렬되어야 함
        // Note: 배경 이미지는 9-sliced 방식이라 자동으로 가로/세로 중앙 정렬임
        Assert.IsTrue(textMesh.verticalAlignment == VerticalAlignmentOptions.Middle);
        Assert.IsTrue(Mathf.Approximately(textMesh.rectTransform.pivot.x, 0.5f));
        Assert.IsTrue(Mathf.Approximately(textMesh.rectTransform.localPosition.x, 0f));

        // 직접 개행 문자를 넣지 않는 이상 대화창이 가로로 쭉 늘어남
        Assert.IsFalse(textMesh.enableWordWrapping);
        Assert.IsTrue(textMesh.overflowMode == TextOverflowModes.Overflow);
    }

    // 설정된 말풍선 꼬리의 위치에 맞는 배경 이미지 선택
    private void ChooseBacgroundSpriteWithCorrectBump()
    {
        if (bumpLocation == BumpLocation.Left)
        {
            backgroundRenderer.sprite = leftBumpBackground;
        }
        else if (bumpLocation == BumpLocation.Right)
        {
            backgroundRenderer.sprite = rightBumpBackground;
        }
        else
        {
            backgroundRenderer.sprite = noBumpBackground;
        }
    }

    public void ShowBox()
    {
        backgroundRenderer.enabled = true;
        textMesh.enabled = true;
    }

    public void HideBox()
    {
        backgroundRenderer.enabled = false;
        textMesh.enabled = false;
    }

    private async UniTask TestDialogueSelection()
    {
        ShowBox();

        List<string> options = new()
        {
            "1. 커피를 마신다",
            "2. 도넛을 먹는다",
            "3. 굶는다"
        };

        int result = await BeginDialogueSelection(options);

        HideBox();

        Debug.Log($"최종 선택: {options[result]}");
    }

    private async UniTask TestDialogueSequence()
    {
        ShowBox();

        await BeginTypingAnimation("하늘을 올려다봐도 하늘밖에 없다");
        await UniTask.WaitForSeconds(1f);
        await BeginTypingAnimation("강은 바다로 넓어지고 사람은 죽음으로 넘치네");
        await UniTask.WaitForSeconds(1f);
        await BeginTypingAnimation("추운 겨울에는 <u>따뜻한 커피</u>와 티를 마셔야지요");
        await UniTask.WaitForSeconds(1f);
        await BeginTypingAnimation("사막에서 걷는 법을 알려줘\n신발에 모래가 들어가서 발을 델 것 같아");
        await UniTask.WaitForSeconds(1f);

        HideBox();
    }

    public async UniTask BeginTypingAnimation(string dialogue)
    {
        textMesh.text = dialogue;
        AdjustBoxSize();

        // 아무 글자도 보이지 않는 상태에서 한 글자씩 출력
        textMesh.maxVisibleCharacters = 0;

        while (textMesh.maxVisibleCharacters < textMesh.text.Length)
        {
            textMesh.maxVisibleCharacters += 1;

            await UniTask.WaitForSeconds(characterAppearanceDelay);
        }
    }

    // 전체 텍스트가 들어갈 수 있도록 배경 이미지의 사이즈를 조정
    private void AdjustBoxSize()
    {
        // 1. 전체 텍스트의 mesh를 준비한다
        textMesh.maxVisibleCharacters = textMesh.text.Length;
        textMesh.ForceMeshUpdate();

        // 2. mesh에 맞게 text 오브젝트의 rectTransform size를 변경한다 (width, height)
        textMesh.rectTransform.sizeDelta = new Vector2(textMesh.renderedWidth, textMesh.renderedHeight);

        // 3. mesh의 크기를 기준으로 약간의 padding을 더해 배경 이미지 크기를 조정한다
        backgroundRenderer.size = new Vector2(textMesh.renderedWidth + widthPadding, textMesh.renderedHeight + heightPadding);

        // 4. 말풍선 꼬리가 제자리에 머물도록 중심을 옮긴다
        var position = Vector2.up * (heightOffset + backgroundRenderer.size.y / 2f);
        if (bumpLocation == BumpLocation.Left)
        {
            position.x += backgroundRenderer.size.x / 2f;
        }
        else if (bumpLocation == BumpLocation.Right)
        {
            position.x -= backgroundRenderer.size.x / 2f;
        }
        transform.localPosition =  position;
    }

    public async UniTask<int> BeginDialogueSelection(List<string> options)
    {
        // 첫 번째 옵션을 기본 선택으로 삼고 대화창 출력
        dialogueOptions = options;
        isSelectingDialogueOption = true;
        selectedDialogueOptionIndex = 0;
        UpdateDialogueOptionText();
        AdjustBoxSize();

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
                newText += $"<b>{dialogueOptions[i]}</b>";
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
