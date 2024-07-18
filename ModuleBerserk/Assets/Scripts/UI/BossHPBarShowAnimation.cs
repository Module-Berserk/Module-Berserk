using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BossHPBarShowAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform sliderRoot;
    [SerializeField] private RectTransform bossName;

    private void OnEnable()
    {
        sliderRoot.DOScaleX(1f, 0.5f).From(0f).SetEase(Ease.OutBounce);
        bossName.DOScaleX(1f, 0.5f).From(0f).SetEase(Ease.OutBounce);
    }
}
