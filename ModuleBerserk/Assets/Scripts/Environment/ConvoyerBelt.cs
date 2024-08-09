using DG.Tweening;
using UnityEngine;

public class ConvoyerBelt : MonoBehaviour
{
    [Header("Platform Movement")]
    // 한 바퀴 도는데 걸리는 시간
    [SerializeField] private float singleCycleDuration = 5f;
    [SerializeField] private Ease platformMovementEase = Ease.InOutSine;


    [Header("Automatic Loops")]
    // 활성화하면 자동으로 무한 왕복을 시작함
    [SerializeField] private bool autoLoop = false;


    [Header("Component References (DO NOT MODIFY)")]
    [SerializeField] private Rigidbody2D movingPlatform;
    [SerializeField] private CapsuleCollider2D convoyerBeltBoundary;


    private Vector2[] path;

    private void Awake()
    {
        Vector2 center = convoyerBeltBoundary.transform.position;
        Vector2 right = convoyerBeltBoundary.transform.right;
        Vector2 up = convoyerBeltBoundary.transform.up;
        Vector2 halfSize = convoyerBeltBoundary.size / 2f;

        float radius = halfSize.y;
        float width = halfSize.x - halfSize.y;

        float sin45 = Mathf.Sqrt(2f) / 2;

        path = new[]
        {
            center - right * (width + radius),
            center - right * (width + radius * sin45) + up * radius * sin45,
            center - right * width + up * radius,
            center + up * radius,
            center + right * width + up * radius,
            center + right * (width + radius * sin45) + up * radius * sin45,
            center + right * (width + radius),
            center + right * (width + radius * sin45) - up * radius * sin45,
            center + right * width - up * radius,
            center - up * radius,
            center - right * width - up * radius,
            center - right * (width + radius * sin45) - up * radius * sin45,
            center - right * (width + radius),
        };
        movingPlatform.position = path[0];
    }

    private void OnDestroy()
    {
        movingPlatform.DOKill();
    }

    private void Start()
    {
        if (autoLoop)
        {
            movingPlatform.DOPath(path, singleCycleDuration, PathType.CatmullRom, PathMode.Sidescroller2D)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear)
                .SetUpdate(UpdateType.Fixed);
        }
    }

    public void MoveSingleCycle()
    {
        movingPlatform.DOPath(path, singleCycleDuration, PathType.CatmullRom, PathMode.Sidescroller2D)
            .SetEase(platformMovementEase)
            .SetUpdate(UpdateType.Fixed);
    }
}
