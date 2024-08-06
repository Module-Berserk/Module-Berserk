using Cinemachine;
using UnityEngine;

// 챕터1 보스의 박격포 패턴 중 포탄 하나의 폭발 애니메이션 및 히트박스 처리 담당
[RequireComponent(typeof(ScreenShake))]
public class C1BossCannonExplode : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float damage;
    [SerializeField] private float cameraShakeForce;

    private ScreenShake screenShake;

    private void Awake()
    {
        screenShake = GetComponent<ScreenShake>();
        GetComponent<ApplyDamageOnContact>().RawDamage = new CharacterStat(damage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 박격포 패턴은 바닥에 떨어진 상자를 부술 수 있음
        if (other.TryGetComponent(out C1BoxGimmick boxGimmick))
        {
            boxGimmick.DestroyBox();
        }
    }

    public void ApplyCameraShake()
    {
        screenShake.ApplyScreenShake(cameraShakeForce, duration: 0.2f);
    }

    // 폭발 애니메이션이 끝나면 호출되는 함수
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
    private void PlayCannonFallSFX() {
        int [] fallIndices = {35};
        AudioManager.instance.PlaySFXBasedOnPlayer(fallIndices, this.transform);
    }
}
