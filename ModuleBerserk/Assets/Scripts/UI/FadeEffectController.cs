using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FadeEffect : MonoBehaviour
{
    [SerializeField] private Image fadeEffectImage;
    [SerializeField] private float fadeDuration;

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
