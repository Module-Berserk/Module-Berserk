using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;


public class MiniTurret : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float initialDelay;
    [SerializeField] private float delayBetweenFire;
    [SerializeField] private int numBullets;
    [SerializeField] private float bulletVelocity;
    private SpriteRenderer spriteRenderer;

    public bool IsFacingLeft
    {
        get => spriteRenderer.flipX;
        protected set => spriteRenderer.flipX = value;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 생성될 때 플레이어가 바라보는 방향을 사용
        var player = FindObjectOfType<PlayerManager>();
        IsFacingLeft = player.IsFacingLeft;

        // 작동 시작!
        BeginWorkingAsync().Forget();
    }

    private async UniTask BeginWorkingAsync()
    {
        // 생성되고 바로 쏘기 시작하면 어색하니까 잠깐 대기
        await UniTask.WaitForSeconds(initialDelay);

        for (int i = 0; i < numBullets; ++i)
        {
            FireBullet();
            await UniTask.WaitForSeconds(delayBetweenFire);
        }

        Destroy(gameObject);
    }

    private void FireBullet()
    {
        // TODO: 총구에서 스폰되도록 위치 조정
        var bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        // 속도 설정 (총알은 중력 0으로 설정되어있음!)
        var rb = bullet.GetComponent<Rigidbody2D>();
        rb.velocity = (IsFacingLeft ? Vector2.left : Vector2.right) * bulletVelocity;

        // 포탑과 충돌하지 않도록 설정
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), bullet.GetComponent<Collider2D>());

        transform.DOShakeScale(duration: 0.2f, strength: 0.5f);
    }
}
