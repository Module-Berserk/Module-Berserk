using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class SmokeGrenadeSlowArea : MonoBehaviour
{
    [SerializeField] private float fadeEffectDuration;
    [SerializeField] private float slowEffectDuration;
    [SerializeField] private float slowRatio;

    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        DestroyAfterDurationAsync().Forget();
    }

    private async UniTask DestroyAfterDurationAsync()
    {
        // 페이드인
        spriteRenderer.DOFade(1f, fadeEffectDuration).From(0f).SetEase(Ease.InSine);

        // 페이드아웃 효과 중에도 슬로우는 들어가니까 나머지 시간 만큼 기다려야 함
        await UniTask.WaitForSeconds(slowEffectDuration - fadeEffectDuration);
        
        // 페이드아웃
        spriteRenderer.DOFade(0f, fadeEffectDuration).From(1f).SetEase(Ease.InSine);

        await UniTask.WaitForSeconds(fadeEffectDuration);

        Destroy(gameObject);
    }

    // 영역에 들어오면 슬로우 디버프 부여
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IMovingObject movingObject))
        {
            CharacterStat moveSpeed = movingObject.GetMoveSpeed();
            moveSpeed.ApplyMultiplicativeModifier(slowRatio);
        }
    }

    // 영역에서 나가면 디버프 제거
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out IMovingObject movingObject))
        {
            CharacterStat moveSpeed = movingObject.GetMoveSpeed();
            moveSpeed.ApplyMultiplicativeModifier(1f / slowRatio);
        }
    }
}
