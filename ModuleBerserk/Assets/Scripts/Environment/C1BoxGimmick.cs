using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Cinemachine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class C1BoxGimmick : MonoBehaviour
{
    // 일반 맵에서는 부수면 크레딧을 드랍해야 하지만
    // 보스전에서는 상자가 무한 리필되므로 단순 파괴만 가능해야 함
    [SerializeField] private bool shouldDropCreditOnDestroy = false;
    // 플레이어가 대쉬로 충돌한 경우 부여할 스턴 지속시간
    [SerializeField] private float playerStunDurationOnDashImpact = 2.0f;
    // 플레이어가 충돌해서 상자가 파괴될 때 부여할 카메라 흔들림 효과 강도
    [SerializeField] private float cameraShakeForce = 0.1f;

    private CinemachineImpulseSource cameraShake;

    private void Awake()
    {
        cameraShake = GetComponent<CinemachineImpulseSource>();
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        bool isCollisionHorizontal = Mathf.Approximately(Vector2.Dot(other.contacts[0].normal, Vector2.up), 0f);
        if (other.gameObject.CompareTag("Player") && isCollisionHorizontal)
        {
            var playerManager = other.gameObject.GetComponent<PlayerManager>();
            Assert.IsNotNull(playerManager);

            // 플레이어가 대쉬로 충돌한 경우 상자 파괴
            if (playerManager.ActionState == PlayerActionState.Evade)
            {
                DestroyBox();
                playerManager.ApplyStunForDurationAsync(playerStunDurationOnDashImpact).Forget();
                cameraShake.GenerateImpulse(cameraShakeForce);
            }
        }

        // TODO: 챕터1 보스의 상자 파괴 공격에 맞은 경우에도 DestroyBox(). 아마 이 경우에는 크레딧 드랍 x
    }

    public void DestroyBox()
    {
        if (shouldDropCreditOnDestroy)
        {
            // TODO: 크레딧이나 아이템 드랍
        }
        Destroy(gameObject);
    }
}
