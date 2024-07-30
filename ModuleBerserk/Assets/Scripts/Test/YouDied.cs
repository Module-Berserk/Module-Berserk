using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class YouDied : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float fadeDuration;
    [SerializeField] private float stayDuration;

    // 유다희 글자를 페이드 효과와 함께 보여줌.
    // 페이드 아웃이 시작될 때 task가 종료된다.
    public async UniTask FadeInoutAsync()
    {
        background.DOFade(1f, fadeDuration).From(0f).SetEase(Ease.OutSine);
        text.DOFade(1f, fadeDuration).From(0f).SetEase(Ease.OutSine);

        await UniTask.WaitForSeconds(fadeDuration + stayDuration);

        background.DOFade(0f, fadeDuration).From(1f).SetEase(Ease.InSine);
        text.DOFade(0f, fadeDuration).From(1f).SetEase(Ease.InSine);
    }
}
