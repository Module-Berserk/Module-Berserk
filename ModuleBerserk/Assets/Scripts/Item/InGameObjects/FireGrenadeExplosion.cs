using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class FireGrenadeExplosion : MonoBehaviour
{
    [SerializeField] private float damage;

    private void Start()
    {
        var hitbox = GetComponent<ApplyDamageOnContact>();
        hitbox.RawDamage = new CharacterStat(damage);

        // TODO: 폭발 애니메이션 나오면 마지막 프레임에 삭제하는 방식으로 변경하기
        AutoDestroy().Forget();
    }

    private async UniTask AutoDestroy()
    {
        transform.DOScale(2f, 0.2f).From(0f).SetEase(Ease.OutExpo);
        await UniTask.WaitForSeconds(0.2f);
        Destroy(gameObject);
    }
}
