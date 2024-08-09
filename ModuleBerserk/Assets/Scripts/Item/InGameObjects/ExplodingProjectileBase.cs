using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class ExplodingProjectileBase : MonoBehaviour
{
    [SerializeField] private float explosionDelay = 0f; // 착지한 후 몇 초 뒤에 터지는지

    private bool isExploded = false;

    private void OnCollisionStay2D(Collision2D other)
    {
        // 바닥이 아니라 벽/천장에는 반응하지 않음
        if (Vector2.Dot(other.GetContact(0).normal, Vector2.up) < 0.1f)
        {
            return;
        }

        // 경사로에서 둘 이상의 콜라이더에 동시에 충돌하는
        // 상황에서도 한 번만 폭발하도록 제한함.
        if (!isExploded)
        {
            isExploded = true;

            ExplodeWithDelay(other).Forget();
        }
    }

    private async UniTask ExplodeWithDelay(Collision2D other)
    {
        await UniTask.WaitForSeconds(explosionDelay);

        OnExplosion(other);

        Destroy(gameObject);
    }

    protected abstract void OnExplosion(Collision2D other);
}
