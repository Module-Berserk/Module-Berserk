using DG.Tweening;
using UnityEngine;

// 양옆으로 움직이는 플랫폼 위에서도 플레이어가 가만히 서있는지 확인하기 위해 만든 스크립트
public class SidewayMovingPlatform : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.DOMoveX(rb.position.x + 4f, 3f).SetLoops(-1, LoopType.Yoyo);
    }
}
