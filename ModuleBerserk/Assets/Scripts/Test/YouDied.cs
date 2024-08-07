using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class YouDied : MonoBehaviour
{
    [Header("External References")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer; // 플레이어의 layer order를 잔뜩 올려서 검은 배경에 플레이어만 보이도록 만듦
    [SerializeField] private List<GameObject> UIElementsToDisable; // HUD, 보스 체력바 등 UI는 사망 연출에 쓰이는 검은 배경보다 위에 그려지므로 직접 비활성화해줘야 함


    [Header("Duration")]
    [SerializeField] private float fadeDuration;
    [SerializeField] private float stayDuration;


    [Header("Internal References (DO NOT MODIFY)")]
    [SerializeField] private SpriteRenderer background; // 플레이어만 남기고 모두 가리는 검은 배경
    [SerializeField] private TextMeshPro text; // 남은 재도전 기회 보여줄 텍스트


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

    // remainingRevives는 부활한 뒤에 다시 재시도할 수 있는 횟수.
    // 이 값이 0 이상이면 일단 부활하는거고 음수면 미션 실패라서 은신처로 돌아가는 상황.
    public async UniTask StartDeathCutsceneAsync(int remainingRevives)
    {
        // 메인 UI 다 가리기
        foreach (var gameObject in UIElementsToDisable)
        {
            gameObject.SetActive(false);
        }

        // 플레이어와 검은 배경만 보이도록 레이어 우선순위를 잔뜩 끌어올리기
        playerSpriteRenderer.sortingOrder = 9999;
        text.sortingOrder = 9998;
        background.sortingOrder = 9997;

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

        await UniTask.WaitForSeconds(fadeDuration, cancellationToken: cancellationTokenSource.Token);

        // 잔여 기회에 따라 미션 실패 안내 문구 또는 잔여 기회를 텍스트로 알려줌
        if (remainingRevives < 0)
        {
            text.text = "- 미션 실패 -\n<size=70%>은신처로 복귀합니다</size>";
        }
        else if (remainingRevives == 0)
        {
            text.text = "- 마지막 재도전 기회 -";
        }
        else
        {
            text.text = $"- 남은 재도전 기회: {remainingRevives} -";
        }
        text.DOFade(1f, fadeDuration)
            .From(0f)
            .SetEase(Ease.InSine);

        await UniTask.WaitForSeconds(stayDuration, cancellationToken: cancellationTokenSource.Token);

        // 텍스트와 주인공 페이드
        playerSpriteRenderer.DOFade(0f, fadeDuration)
            .From(1f)
            .SetEase(Ease.InSine);
        text.DOFade(0f, fadeDuration)
            .From(1f)
            .SetEase(Ease.OutSine);

        await UniTask.WaitForSeconds(fadeDuration, cancellationToken: cancellationTokenSource.Token);
    }
}
