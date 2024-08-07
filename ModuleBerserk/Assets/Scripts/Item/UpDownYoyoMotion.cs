using DG.Tweening;
using UnityEngine;

// 드랍템같은 오브젝트의 부드럽게 위아래로 움직이는 모션을 부여해주는 컴포넌트
public class UpDownYoyoMotion : MonoBehaviour
{
    [SerializeField] private float motionHeight = 0.3f;
    [SerializeField] private float motionDuration = 1f;

    private void Start()
    {
        transform.DOMoveY(transform.position.y + motionHeight, motionDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDestroy()
    {
        // 드랍 아이템의 위아래 움직임은 무한지속이라 transform이 삭제될 때 같이 취소해줘야함
        transform.DOKill();
    }
}
