using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 유니콘에서 임시로 사용할 튜토리얼 파트에 사용할 연출.
// 행사 끝나면 폐기할 예정.
public class TutorialController : MonoBehaviour
{
    [SerializeField] private FadeEffect fadeEffect;
    [SerializeField] private Image textBackgroundImage;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private GearSystem gearSystem;
    [SerializeField] private GameObject stage1EntranceWall;

    static bool isIntroCutscenePlayed = false;

    private void Start()
    {
        // 처음으로 미션에 진입하는 경우
        if (!isIntroCutscenePlayed)
        {
            isIntroCutscenePlayed = true;
            ShowTutorialStartCutsceneAsync().Forget();
        }
        // 죽었다가 부활한 경우
        else
        {
            fadeEffect.FadeIn();
            gearSystem.enabled = true; // 기어 게이지 ramp up 시작
        }
    }

    private async UniTask ShowTutorialStartCutsceneAsync()
    {
        InputManager.InputActions.Player.Disable();

        // 1. "튜토리얼 메모리를 재생합니다" 화면에 띄우기
        text.DOFade(1f, 1f).From(0f).SetEase(Ease.OutSine);
        textBackgroundImage.DOFade(1f, 1f).From(0f).SetEase(Ease.OutSine);

        // 2. 잠깐 기다렸다가 글자 슘기기
        await UniTask.WaitForSeconds(2f);
        text.DOFade(0f, 1f).From(1f).SetEase(Ease.InSine);
        textBackgroundImage.DOFade(0f, 1f).From(1f).SetEase(Ease.InSine);
        
        // 3. 페이드 인
        await UniTask.WaitForSeconds(2f);
        fadeEffect.FadeIn();
        gearSystem.enabled = true; // 기어 게이지 ramp up 시작

        InputManager.InputActions.Player.Enable();
    }

    // 튜토리얼 구간 마지막에 배치된 엘리베이터 레버를 작동하면 호출되는 함수.
    // 챕터1 시작 지점으로 이동하는 동안 페이드 아웃하고 튜토리얼 끝났다고 알려줌.
    public void OnTutorialFinish()
    {
        ShowTutorialFinishCutsceneAsync().Forget();
    }

    private async UniTask ShowTutorialFinishCutsceneAsync()
    {
        InputManager.InputActions.Player.Disable();

        // 1. 엘리베이터 움직이는거 잠깐 보여주고 페이드 아웃
        await UniTask.WaitForSeconds(3f);
        fadeEffect.FadeOut();

        // 2. 튜토리얼 끝났다고 화면에 띄우기
        await UniTask.WaitForSeconds(1f);
        text.text = "메모리 재생이 종료되었습니다\n전투를 개시합니다";
        text.DOFade(1f, 1f).From(0f).SetEase(Ease.OutSine);
        textBackgroundImage.DOFade(1f, 1f).From(0f).SetEase(Ease.OutSine);

        // 3. 잠깐 기다렸다가 글자 슘기기
        await UniTask.WaitForSeconds(2f);
        text.DOFade(0f, 1f).From(1f).SetEase(Ease.InSine);
        textBackgroundImage.DOFade(0f, 1f).From(1f).SetEase(Ease.InSine);
        
        // 4. 페이드 인
        await UniTask.WaitForSeconds(2f);
        fadeEffect.FadeIn();
        
        // 5. 엘리베이터 완전히 정차할 때까지 기다렸다가 입구 막기
        await UniTask.WaitForSeconds(2f);
        stage1EntranceWall.SetActive(true);

        InputManager.InputActions.Player.Enable();
    }
}
