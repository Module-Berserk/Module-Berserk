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
    private Animator animator;

    public bool IsFacingLeft
    {
        get => spriteRenderer.flipX;
        protected set => spriteRenderer.flipX = value;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

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
            animator.SetTrigger("Fire");
            await UniTask.WaitForSeconds(delayBetweenFire);
        }

        Destroy(gameObject);
    }

    // 발사 애니메이션에서 호출되는 함수.
    private void FireBullet()
    {
        // 총구에서 스폰되도록 위치 조정
        Vector2 spawnPosition = transform.position + (IsFacingLeft ? Vector3.left : Vector3.right) * 0.11f;
        var bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

        // 속도 설정 (총알은 중력 0으로 설정되어있음!)
        var rb = bullet.GetComponent<Rigidbody2D>();
        rb.velocity = (IsFacingLeft ? Vector2.left : Vector2.right) * bulletVelocity;

        // 포탑과 충돌하지 않도록 설정
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), bullet.GetComponent<Collider2D>());
        int[] turretIndices = {39};
        AudioManager.instance.PlaySFXBasedOnPlayer(turretIndices, this.transform);
        // transform.DOShakeScale(duration: 0.2f, strength: 0.5f);
    }
}
