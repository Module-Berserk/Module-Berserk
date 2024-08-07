using Cinemachine;
using UnityEngine;

// 한 맵 안에서도 스테이지 영역마다 다른 virtual camera를 사용하므로
// 세이브 데이터를 로딩할 때 플레이어가 스폰되는 영역에 해당하는
// 카메라의 priority를 스테이지 영역을 이동할 때와 동일하게 높게 올려줘야 함.
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class FollowCameraState : ObjectGUID, IPersistentSceneState
{
    public const int INACTIVE_CAMERA_PRIORITY = 5;
    public const int ACTIVE_CAMERA_PRIORITY = 10;

    private CinemachineVirtualCamera virtualCamera;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    void IPersistentSceneState.Load(SceneState sceneState)
    {
        // 아주 드물게 테스트 도중에 이 스크립트의 Awake()보다 먼저 Load()가 실행되는 경우가 발생함.
        // 이 경우 null reference exception이 발생하므로 여기서 확실히 초기화해줘야 함.
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
        }

        // 기록된 태그가 자신과 일치한다면 스스로를 활성화된 카메라로 변경
        if (ID == sceneState.ActiveVirtualCameraGUID)
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
            sceneState.ActiveVirtualCameraGUID = ID;
        }
    }
}
