using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Localization.PropertyVariants.TrackedObjects;

[RequireComponent(typeof(CapsuleCollider2D))]
public class ConvoyerBelt : MonoBehaviour
{
    [SerializeField] private Rigidbody2D movingPlatform;

    private CapsuleCollider2D convoyerBeltBoundary;

    private void Awake()
    {
        convoyerBeltBoundary = GetComponent<CapsuleCollider2D>();
    }

    public void Start()
    {
        var boundary = GetComponent<CapsuleCollider2D>();
        Vector2 center = boundary.transform.position;
        Vector2 right = boundary.transform.right;
        Vector2 up = boundary.transform.up;
        Vector2 halfSize = boundary.size / 2f;

        float radius = halfSize.y;
        float width = halfSize.x - halfSize.y;

        float sin45 = Mathf.Sqrt(2f) / 2;

        Vector2[] path =
        {
            center - right * (width + radius),
            center - right * (width + radius * sin45) + up * radius * sin45,
            center - right * width + up * radius,
            center + up * radius,
            center + right * width + up * radius,
            center + right * (width + radius * sin45) + up * radius * sin45,
            center + right * (width + radius),
        };
        movingPlatform.position = path[0];
        movingPlatform.DOPath(path, 5f, PathType.CatmullRom, PathMode.Sidescroller2D, 10, gizmoColor: Color.red)
        .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
}
