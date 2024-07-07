using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 챕터1 보스전 인트로 컷신에서 보스 및 플레이어의 대사 출력과
// 플레이어 입력 비활성화/활성화를 담당하는 클래스.
//
// 타임라인이 signal을 보내주면 Signal Receiver 컴포넌트가
// 이 스크립트의 함수들을 적절히 호출하는 방식으로 작동함.
public class C1BossIntroCutsceneController : MonoBehaviour
{
    [SerializeField] private DialogueBox playerDialogueBox;
    [SerializeField] private DialogueBox bossDialogueBox;
    [SerializeField] private C1BossController bossController;

    private enum Speaker
    {
        Player,
        Boss,
    }
    private struct Dialogue
    {
        public Speaker speaker;
        public string message;

        // 대사 이후에 대화창이 숨겨지길 원하면 true를,
        // 곧바로 다음 대사를 출력하기를 원하면 false를 주면 됨.
        // Note: 같은 사람이 대사를 연속으로 출력하는 경우 대화창을 껐다가 켜면 이상해보임!
        public bool hideBoxOnComplete;
    }
    private List<Dialogue> dialogues = new()
    {
        new Dialogue{speaker = Speaker.Boss, message = "하, 이번에는 웬 고철덩이가 찾아왔군", hideBoxOnComplete = true},
        new Dialogue{speaker = Speaker.Player, message = "허밍버드가 이곳에 있다던데...", hideBoxOnComplete = true},
        new Dialogue{speaker = Speaker.Boss, message = "그래, 너도 이게 목적인 모양이군", hideBoxOnComplete = false},
        new Dialogue{speaker = Speaker.Boss, message = "그럼 어디 실력 좀 볼까?", hideBoxOnComplete = true},
    };
    private int nextDialogueIndex = 0;

    // 컷신의 시작과 끝에 호출되는 함수.
    // 잠시 플레이어 조작을 막아준다.
    public void DisablePlayerControl()
    {
        InputManager.InputActions.Player.Disable();
    }

    public void EnablePlayerControl()
    {
        InputManager.InputActions.Player.Enable();
    }

    // 인트로 컷신 끝나고 보스 AI를 활성화시킬 때 호출됨
    public void EnableBossController()
    {
        bossController.enabled = true;
    }

    // 다음 대사가 출력되어야 할 타이밍에 호출됨
    public void BeginNextDialogue()
    {
        // 화자에 맞게 대사창 선택 및 대사 출력
        Dialogue nextDialogue = dialogues[nextDialogueIndex++];
        DialogueBox dialogueBox = nextDialogue.speaker == Speaker.Boss ? bossDialogueBox : playerDialogueBox;
        int[] talkingIndices = nextDialogue.speaker == Speaker.Boss ? new int[] { 19 } : new int[] { 20 };
        AudioManager.instance.PlaySFX(talkingIndices);
        BeginTypingAnimationAsync(dialogueBox, nextDialogue).Forget();
    }

    private async UniTask BeginTypingAnimationAsync(DialogueBox dialogueBox, Dialogue dialogue)
    {
        dialogueBox.ShowBox();
        await dialogueBox.BeginTypingAnimation(dialogue.message);

        if (dialogue.hideBoxOnComplete)
        {
            // 대사 출력 끝나도 계속 읽을 수 있도록 잠깐 대기
            await UniTask.WaitForSeconds(1f);
            dialogueBox.HideBox();
        }
    }
}
