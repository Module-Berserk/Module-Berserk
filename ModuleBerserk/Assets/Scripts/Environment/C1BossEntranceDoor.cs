using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

// 챕터1 맵의 보스방으로 진입하는 문.
// 상호작용을 하면 문이 열리면서 컷신이 시작된다.
// 세이브 포인트 역할도 겸사겸사 수행함.
[RequireComponent(typeof(Animator))]
public class C1BossEntranceDoor : SavePoint, IInteractable
{
    [SerializeField] private PlayableDirector bossIntroCutscene;
    [SerializeField] private TextMeshPro text;

    // 이미 한 번 컷신을 보고 부활한 상태에서 컷신 없이
    // 즉시 보스전을 시작하기 위해 필요한 레퍼런스들
    [Header("Intro Cutscene Skip")]
    [SerializeField] private Transform playerStart;
    [SerializeField] private C1BossController bossController;
    [SerializeField] private GameObject bossHPBarUI;
    [SerializeField] private GameObject bossRoomFixedCamera;
    [SerializeField] private FadeEffect fadeEffect;

    // 문 열리는 연출 도중에 또 상호작용 하는 상황 방지
    private bool isActivated = false;

    void IInteractable.OnPlayerEnter()
    {
        text.enabled = true;
    }

    void IInteractable.OnPlayerExit()
    {
        text.enabled = false;
    }

    void IInteractable.StartInteraction()
    {
        // 중복 재생 방지
        if (!isActivated)
        {
            isActivated = true;

            // 일단 플레이어 입력 막고 문 열리는 애니메이션 재생.
            // 보스전 또는 컷신 시작은 문 다 열리고 나서 애니메이션 이벤트로 처리된다.
            InputManager.InputActions.Player.Disable();
            GetComponent<Animator>().SetTrigger("Open");
        }
    }

    // 문 열리는 애니메이션 마지막에 이벤트로 호출되는 함수
    public void OnDoorOpenEnd()
    {
        // case 1) 한 번 죽고 부활해서 재시도하는 경우 => 컷신 이미 봤으니까 스킵하고 바로 보스전 시작
        if (GameStateManager.ActiveGameState.SceneState.IsBossIntroCutscenePlayed)
        {
            StartBossFightWithoutCutsceneAsync().Forget();
        }
        // case 2) 이번 미션에서 최초로 보스방에 진입하는 경우 => 인트로 컷신 시작
        else
        {
            // 컷신 봤다고 기록
            GameStateManager.ActiveGameState.SceneState.IsBossIntroCutscenePlayed = true;
            GameStateManager.ActiveGameState.PlayerState.SpawnPointTag = gameObject.tag;
            GameStateManager.SaveActiveGameState();

            bossIntroCutscene.Play();
        }
    }

    private async UniTask StartBossFightWithoutCutsceneAsync()
    {
        // 페이드 아웃
        fadeEffect.FadeOut();
        await UniTask.WaitForSeconds(1f);

        // 플레이어와 카메라를 보스전 시작 위치로 옮기기
        GameObject.FindGameObjectWithTag("Player").transform.position = playerStart.position;
        bossRoomFixedCamera.SetActive(true);

        await UniTask.WaitForSeconds(2f);

        // 페이드 인
        fadeEffect.FadeIn();
        await UniTask.WaitForSeconds(1f);

        // 플레이어와 보스 모두 활성화하며 전투 시작
        InputManager.InputActions.Player.Enable();
        bossController.enabled = true;
        bossHPBarUI.SetActive(true);

        // TODO: 브금 교체 (진하)
    }
}
