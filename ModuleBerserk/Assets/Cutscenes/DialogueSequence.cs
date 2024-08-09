using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;


public class DialogueSequence : MonoBehaviour
{
    [Serializable]
    private class DialogueSequenceEntry
    {
        public DialogueBox dialogueBox;
        public string localizationKey;
        public bool hideBoxOnComplete;

        public string GetLocalizedDialogue()
        {
            return dialogueBox.GetLocalizedDialogue(localizationKey);
        }
    }

    [SerializeField] private List<DialogueSequenceEntry> dialogues;

    private int nextDialogueIndex = 0;

    // 다음 대사가 출력되어야 할 타이밍에 호출됨
    public void BeginNextDialogue()
    {
        Assert.IsTrue(nextDialogueIndex < dialogues.Count);

        DialogueSequenceEntry nextDialogue = dialogues[nextDialogueIndex++];
        BeginTypingAnimationAsync(nextDialogue).Forget();
    }

    private async UniTask BeginTypingAnimationAsync(DialogueSequenceEntry dialogueEntry)
    {
        dialogueEntry.dialogueBox.ShowBox();

        string message = dialogueEntry.GetLocalizedDialogue();
        await dialogueEntry.dialogueBox.BeginTypingAnimation(message);

        if (dialogueEntry.hideBoxOnComplete)
        {
            // 대사 출력 끝나도 계속 읽을 수 있도록 잠깐 대기
            await UniTask.WaitForSeconds(1f);
            dialogueEntry.dialogueBox.HideBox();
        }
    }

    private void Awake()
    {
        InputManager.InputActions.Player.UpArrow.performed += (context) =>{
            BeginNextDialogue();
        };
    }
}
