using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class YouDied : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float fadeDuration;

    private void OnEnable()
    {
        background.DOFade(0.5f, fadeDuration).From(0f).SetEase(Ease.InSine);
        text.DOFade(1f, fadeDuration).From(0f).SetEase(Ease.InSine);
    }
}
