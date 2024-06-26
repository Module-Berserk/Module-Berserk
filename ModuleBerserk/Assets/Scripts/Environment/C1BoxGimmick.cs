using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(BoxCollider2D))]
public class C1BoxGimmick : MonoBehaviour
{
    // 일반 맵에서는 부수면 크레딧을 드랍해야 하지만
    // 보스전에서는 상자가 무한 리필되므로 단순 파괴만 가능해야 함
    [SerializeField] private bool shouldDropCreditOnDestroy = false;
    // 플레이어가 대쉬로 충돌한 경우 부여할 스턴 지속시간
    [SerializeField] private float playerStunDurationOnDashImpact = 2.0f;

    private void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log("플레이어가 상자에 대쉬함");
        if (other.gameObject.CompareTag("Player"))
        {
            var playerManager = other.gameObject.GetComponent<PlayerManager>();
            Assert.IsNotNull(playerManager);

            // 플레이어가 대쉬로 충돌한 경우 상자 파괴
            if (playerManager.ActionState == PlayerActionState.Evade)
            {
                DestroyBox();
                playerManager.ApplyStunForDurationAsync(playerStunDurationOnDashImpact).Forget();
                // TODO: 카메라 흔들림 넣기
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
