using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

// 챕터1 보스전 컷신에서 보스 및 플레이어의 대사 출력과
// 플레이어 입력 비활성화/활성화를 담당하는 클래스.
//
// 타임라인이 signal을 보내주면 Signal Receiver 컴포넌트가
// 이 스크립트의 함수들을 적절히 호출하는 방식으로 작동함.
public class C1BossCutsceneController : MonoBehaviour
{
    [SerializeField] private DialogueBox playerDialogueBox;
    [SerializeField] private DialogueBox bossDialogueBox;
    [SerializeField] private C1BossController bossController;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private SpriteRenderer bossSpriteRenderer;

    [Header("Ending Cutscenes")]
    [SerializeField] private PlayableDirector mercyEnding;
    [SerializeField] private PlayableDirector killEnding;

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

    // 플레이어와 보스의 스프라이트 방향을 원하는대로 고정하기 위해 사용되는 함수.
    public void SetPlayerSpriteRight()
    {
        playerSpriteRenderer.flipX = false;
    }

    public void SetPlayerSpriteLeft()
    {
        playerSpriteRenderer.flipX = true;
    }

    public void SetBossSpriteRight()
    {
        bossSpriteRenderer.flipX = false;
    }

    public void SetBossSpriteLeft()
    {
        bossSpriteRenderer.flipX = true;
    }

    // 인트로 컷신 끝나고 보스 AI를 활성화시킬 때 호출됨
    public void EnableBossController()
    {
        bossController.enabled = true;
    }

    // 죽인다 살린다 선택지 띄우고 후속 컷신 재생
    public void BeginEndingSelection()
    {
        SelectEndingAsync().Forget();
    }

    private async UniTask SelectEndingAsync()
    {
        List<string> options = new()
        {
            "1. 마무리한다",
            "2. 자비를 베푼다"
        };

        playerDialogueBox.ShowBox();
        int selectedEndingIndex = await playerDialogueBox.BeginDialogueSelection(options);
        playerDialogueBox.HideBox();

        if (selectedEndingIndex == 0)
        {
            killEnding.Play();
        }
        else
        {
            mercyEnding.Play();
        }
    }
}
