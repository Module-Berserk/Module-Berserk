using UnityEngine;

public abstract class ExplodingProjectileBase : MonoBehaviour
{
    private bool isExploded = false;

    private void OnCollisionStay2D(Collision2D other)
    {
        // 바닥이 아니라 벽/천장에는 반응하지 않음
        if (Vector2.Dot(other.contacts[0].normal, Vector2.up) < 0.1f)
        {
            return;
        }

        // 경사로에서 둘 이상의 콜라이더에 동시에 충돌하는
        // 상황에서도 한 번만 폭발하도록 제한함.
        if (!isExploded)
        {
            isExploded = true;

            OnExplosion(other);

            Destroy(gameObject);
        }
    }

    protected abstract void OnExplosion(Collision2D other);
}
