using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// PlayerDetector는 플레이어를 인식하고 인식 정보를
// 주변에 공유할 수 있는 대상을 나타내는 인터페이스임.
//
// 모종의 방법으로 본인이 플레이어를 최초로 감지한 시점에
// ShareDetectionInfo()를 호출해주면 자동으로 주변에 감지 정보를 공유함.
public interface IPlayerDetector
{
    // 인식 공유 범위 (원형 범위의 반지름)
    float GetDetectionSharingRadius();

    // 인식 공유 범위의 중심이 될 현재 위치
    Vector2 GetPosition();

    // 플레이어를 직접 인식하거나 주변에서 인식 정보를 공유받은 경우 단발성으로 호출되는 함수
    void HandlePlayerDetection();

    // 자신이 최초로 플레이어를 인식한 경우 호출해줘야 하는 함수.
    // 주위에 있는 IPlayerDetector에게 정보를 공유해
    // HandlePlayerDetection()이 호출되게 만든다.
    void ShareDetectionInfo()
    {
        var colliders = Physics2D.OverlapCircleAll(GetPosition(), GetDetectionSharingRadius());
        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out IPlayerDetector detector))
            {
                detector.HandlePlayerDetection();
            }
        }
    }
}
