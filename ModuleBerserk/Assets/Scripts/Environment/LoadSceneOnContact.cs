using UnityEngine;
using UnityEngine.SceneManagement;

// 다른 scene으로 이동하게 만드는 포탈
public class LoadSceneOnContact : MonoBehaviour
{
    [SerializeField] private string sceneName;

    // 목적지 scene에서 플레이어가 시작해야 할 위치를 나타내는 오브젝트의 태그.
    // 플레이어는 맵이 로딩되면 해당 태그를 가진 오브젝트의 위치로 순간이동한다.
    [SerializeField] private string playerSpawnPointTag;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 다음 scene의 플레이어 시작 위치 지정
            GameStateManager.ActiveGameState.PlayerState.SpawnPointTag = playerSpawnPointTag;

            SceneManager.LoadSceneAsync(sceneName);

            // TODO: fading 등 로딩 화면 처리하기
            // TODO: SceneManager.sceneUnloaded에 DOTween.KillAll() 호출하는 콜백 함수 넣기
        }
    }
}
