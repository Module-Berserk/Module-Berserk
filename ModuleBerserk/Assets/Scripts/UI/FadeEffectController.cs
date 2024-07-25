using System.Threading;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FadeEffect : MonoBehaviour
{
    [SerializeField] private Image fadeEffectImage;
    [SerializeField] private float fadeDuration;
    [SerializeField] private bool fadeInOnAwake = true; // true로 체크하면 시작할 때 자동으로 페이드인

    private void Awake()
    {
        if (fadeInOnAwake)
        {
            FadeIn();
        }
    }

    private void OnDestroy()
    {
        // 일어날 확률은 낮지만 페이드 도중에 맵을 전환하게 되면
        // tween warning이 뜨는 문제가 생길 수 있으니 안전하게 정리.
        fadeEffectImage.DOKill();
    }

    public void FadeIn()
    {
        fadeEffectImage.DOFade(0f, fadeDuration);
    }

    public void FadeInImmediate()
    {
        fadeEffectImage.DOFade(0f, 0.01f);
    }
    
    public void FadeOut()
    {
        fadeEffectImage.DOFade(1f, fadeDuration);
    }

    public void FadeOutImmediate()
    {
        fadeEffectImage.DOFade(1f, 0.01f);
    }
}
