using Cinemachine;
using UnityEngine;

// 한 맵 안에서도 스테이지 영역마다 다른 virtual camera를 사용하므로
// 세이브 데이터를 로딩할 때 플레이어가 스폰되는 영역에 해당하는
// 카메라의 priority를 스테이지 영역을 이동할 때와 동일하게 높게 올려줘야 함.
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class FollowCameraState : MonoBehaviour, IPersistentSceneState
{
    public const int INACTIVE_CAMERA_PRIORITY = 5;
    public const int ACTIVE_CAMERA_PRIORITY = 10;

    private CinemachineVirtualCamera virtualCamera;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();

        if (gameObject.CompareTag("Untagged"))
        {
            Debug.LogError("FollowCamera에는 고유한 태그가 할당되어야 함!!!");
        }
    }

    void IPersistentSceneState.Load(SceneState sceneState)
    {
        // 기록된 태그가 자신과 일치한다면 스스로를 활성화된 카메라로 변경
        if (gameObject.CompareTag(sceneState.ActiveVirtualCameraTag))
        {
            virtualCamera.Priority = ACTIVE_CAMERA_PRIORITY;
        }
        else
        {
            virtualCamera.Priority = INACTIVE_CAMERA_PRIORITY;
        }
    }

    void IPersistentSceneState.Save(SceneState sceneState)
    {
        // 자신이 현재 활성화된 카메라라면 자신의 태그를 기록
        CinemachineBrain cinemachineBrain = CinemachineCore.Instance.GetActiveBrain(0);
        bool isMainCamera = cinemachineBrain.ActiveVirtualCamera as CinemachineVirtualCamera == virtualCamera;
        if (isMainCamera)
        {
            sceneState.ActiveVirtualCameraTag = gameObject.tag;
        }
    }
}
