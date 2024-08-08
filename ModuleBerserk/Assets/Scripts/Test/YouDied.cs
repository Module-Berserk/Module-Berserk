using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class YouDied : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playerSpriteRenderer; // 플레이어의 layer order를 잔뜩 올려서 검은 배경에 플레이어만 보이도록 만듦
    [SerializeField] private SpriteRenderer background; // 플레이어만 남기고 모두 가리는 검은 배경
    [SerializeField] private GameObject HUD;
    [SerializeField] private float fadeDuration;
    [SerializeField] private float stayDuration;

    private CancellationTokenSource cancellationTokenSource = new();
    private DG.Tweening.Core.TweenerCore<float, float, DG.Tweening.Plugins.Options.FloatOptions> bulletTimeTweeningHandle = null;

    // scene 이동할 때 tweening과 async task 정리
    private void OnDestroy()
    {
        background.DOKill();
        if (bulletTimeTweeningHandle != null)
        {
            bulletTimeTweeningHandle.Kill();
        }

        cancellationTokenSource.Cancel();
    }

    private void FixedUpdate()
    {
        // world space UI라서 항상 카메라 중앙으로 옮겨줘야 함.
        // Vector2로 변환하는 이유는 카메라의 z축 위치가
        // 0이 아닌 경우에 검은 배경이 제대로 그려지지 않기 때문.
        transform.position = (Vector2)Camera.main.transform.position;
    }

    // 유다희 글자를 페이드 효과와 함께 보여줌
    public async UniTask StartDeathCutsceneAsync()
    {
        // 메인 UI 다 가리기
        HUD.SetActive(false);

        // 플레이어와 검은 배경만 보이도록 레이어 우선순위를 잔뜩 끌어올리기
        playerSpriteRenderer.sortingOrder = 9999;
        background.sortingOrder = playerSpriteRenderer.sortingOrder - 1;

        // 플레이어 사망이 슬로우모션으로 보이도록 timescale 조정
        bulletTimeTweeningHandle = DOTween.To(() => Time.timeScale, (value) => Time.timeScale = value, 1f, fadeDuration + stayDuration)
            .From(0.5f)
            .SetEase(Ease.InExpo)
            .SetUpdate(isIndependentUpdate: true);
        
        // 주인공 제외하고 페이드 아웃
        background.DOFade(1f, fadeDuration)
            .From(0f)
            .SetEase(Ease.OutSine)
            .SetUpdate(isIndependentUpdate: true);

        await UniTask.WaitForSeconds(fadeDuration + stayDuration, cancellationToken: cancellationTokenSource.Token);

        // 주인공 페이드
        playerSpriteRenderer.DOFade(0f, fadeDuration).From(1f).SetEase(Ease.InSine);
        int[] deathIndices = {47};
        AudioManager.instance.PlaySFXBasedOnPlayer(deathIndices, this.transform);

        await UniTask.WaitForSeconds(fadeDuration);
    }
}
