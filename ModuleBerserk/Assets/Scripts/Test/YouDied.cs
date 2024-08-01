using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class YouDied : MonoBehaviour
{
    [SerializeField] private Image primaryBackground; // 화면을 꽉 채우는 검은 배경
    [SerializeField] private Image secondaryBackground; // 초반에 텍스트를 강조하기 위해 사용하는 세로로 좁은 검은 배경
    [SerializeField] private TextMeshProUGUI text; // 유다희 텍스트
    [SerializeField] private float fadeDuration;
    [SerializeField] private float stayDuration;

    private CancellationTokenSource cancellationTokenSource = new();

    // scene 이동할 때 tweening과 async task 정리
    private void OnDestroy()
    {
        primaryBackground.DOKill();
        secondaryBackground.DOKill();
        text.DOKill();

        cancellationTokenSource.Cancel();
    }

    // 유다희 글자를 페이드 효과와 함께 보여줌
    public async UniTask FadeInoutAsync()
    {
        secondaryBackground.DOFade(1f, fadeDuration).From(0f).SetEase(Ease.OutSine);
        text.DOFade(1f, fadeDuration).From(0f).SetEase(Ease.OutSine);

        await UniTask.WaitForSeconds(fadeDuration + stayDuration, cancellationToken: cancellationTokenSource.Token);

        primaryBackground.DOFade(1f, fadeDuration).From(0f).SetEase(Ease.OutSine);
        secondaryBackground.DOFade(0f, fadeDuration).From(1f).SetEase(Ease.InSine);
        text.DOFade(0f, fadeDuration).From(1f).SetEase(Ease.InSine);

        await UniTask.WaitForSeconds(fadeDuration);
    }
}
