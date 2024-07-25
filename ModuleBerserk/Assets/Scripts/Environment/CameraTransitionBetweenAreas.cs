using Cinemachine;
using UnityEngine;

// 한 scene 안에서도 스테이지로 분리된 영역은
// Cinemachine의 confiner 익스텐션을 사용해 카메라가 침범할 수 없도록 함.
// 다른 영역에 진입할 때에 기존 카메라에서 새로운 영역의 카메라로 전환하며
// virtual camera의 priority 설정 등을 해줘야 하는데, 이 스크립트가 그 기능을 제공함
//
// PlayerContactTrigger의 OnActivate에 콜백으로 등록되는 형식으로 사용될 것임.
public class CameraTransitionBetweenAreas : MonoBehaviour
{
    public void BeginAreaTransition(CinemachineVirtualCamera newAreaCamera)
    {
        // 기존 카메라의 priority를 낮추고
        CinemachineBrain cinemachineBrain = CinemachineCore.Instance.GetActiveBrain(0);
        var activeCamera = cinemachineBrain.ActiveVirtualCamera as CinemachineVirtualCamera;
        activeCamera.Priority = FollowCameraState.INACTIVE_CAMERA_PRIORITY;

        // 새로운 카메라의 priority를 올려주면 자연스럽게 blending이 일어남
        newAreaCamera.Priority = FollowCameraState.ACTIVE_CAMERA_PRIORITY;
    }
}
