using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

// 다른 scene으로 이동하게 만드는 포탈
public class LoadSceneOnContact : MonoBehaviour
{
    [SerializeField] private string sceneName;

    // 목적지 scene에서 플레이어가 시작해야 할 위치를 나타내는 오브젝트의 태그.
    // 플레이어는 맵이 로딩되면 해당 태그를 가진 오브젝트의 위치로 순간이동한다.
    [SerializeField] private string playerSpawnPointTag;
    [SerializeField] private FadeEffect fadeEffect;

    private bool isLoading = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isLoading)
        {
            isLoading = true;
            SceneTransitionAsync().Forget();
        }
    }

    private async UniTask SceneTransitionAsync()
    {
        // 다음 scene의 플레이어 시작 위치 지정
        GameStateManager.ActiveGameState.PlayerState.SpawnPointTag = playerSpawnPointTag;
        GameStateManager.ActiveGameState.SceneState.InitializeSceneState(sceneName);

        fadeEffect.FadeOut();

        // 페이드 효과 끝나면 로딩 시작
        await UniTask.WaitForSeconds(0.5f);
        await SceneManager.LoadSceneAsync(sceneName);

        // TODO: SceneManager.sceneUnloaded에 DOTween.KillAll() 호출하는 콜백 함수 넣기
    }
}
