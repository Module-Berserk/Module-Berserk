using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionStartNPC : MonoBehaviour, IInteractable
{
    // 선택한 의뢰를 시작할 때 사용할 페이드 효과
    [SerializeField] private FadeEffect fadeEffect;
    // 플레이어가 상호작용 범위에 들어오면 보여줄 텍스트
    [SerializeField] private GameObject interactionText;

    void IInteractable.OnPlayerEnter()
    {
        interactionText.SetActive(true);
    }

    void IInteractable.OnPlayerExit()
    {
        interactionText.SetActive(false);
    }

    void IInteractable.StartInteraction()
    {
        StartMissionAsync().Forget();
    }

    private async UniTask StartMissionAsync()
    {
        // 미션 상태 초기화 (ex. 부활 가능 횟수, 오브젝트 파괴 현황 등 세이브 데이터에 들어갈 내용들)
        // SpawnPointTag는 null로 설정해줘야 scene에 배치된 플레이어 오브젝트의 기본 위치를 그대로 사용한다.
        GameStateManager.ActiveGameState.PlayerState.SpawnPointTag = null;
        GameStateManager.ActiveGameState.PlayerState.GearSystemState.NeedInitialRampUp = true;
        GameStateManager.ActiveGameState.SceneState.InitializeSceneState(GameStateManager.ActiveGameState.NextMissionSceneName);

        // TODO: 마차 타고 떠나는 연출 보여주기

        InputManager.InputActions.Player.Disable();

        fadeEffect.FadeOut();
        await UniTask.WaitForSeconds(1f);
        await SceneManager.LoadSceneAsync(GameStateManager.ActiveGameState.NextMissionSceneName);

        InputManager.InputActions.Player.Enable();
    }
}
