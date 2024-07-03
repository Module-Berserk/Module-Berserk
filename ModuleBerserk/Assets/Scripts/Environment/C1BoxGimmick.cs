using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Cinemachine;


[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(CinemachineImpulseSource))]
public class C1BoxGimmick : MonoBehaviour, IDestructible
{
    // 일반 맵에서는 부수면 크레딧을 드랍해야 하지만
    // 보스전에서는 상자가 무한 리필되므로 단순 파괴만 가능해야 함
    [SerializeField] private bool shouldDropCreditOnDestroy = false;
    // 플레이어가 대쉬로 충돌한 경우 부여할 스턴 지속시간
    [SerializeField] private float playerStunDurationOnDashImpact = 2.0f;
    // 플레이어가 충돌해서 상자가 파괴될 때 부여할 카메라 흔들림 효과 강도
    [SerializeField] private float cameraShakeForce = 0.1f;
    // 보스전 선반 위에 있다가 플레이어의 공격에 의해 낙하할 때 옆으로 밀려나는 정도
    [SerializeField] private float pushVelocity = 2f;

    private Rigidbody2D rb;
    private CinemachineImpulseSource cameraShake;

    // 플레이어의 공격에 밀려나게 만들기 위한 IDestructible 요구사항.
    // 방어력에 의한 데미지 감소를 100%로 설정되기 때문에 플레이어가 공격으로 파괴할 수는 없다.
    private CharacterStat hp;
    private CharacterStat defense;

    private const float BOX_GIMMICK_HP = 123456789; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cameraShake = GetComponent<CinemachineImpulseSource>();

        // IDestructible 인터페이스에 필요해서 만들기는 하지만
        // 실제로 데미지 처리를 하지는 않으므로 수치는 중요하지 않음.
        // Note: 박스 기믹은 보스의 박스 제거 공격 또는 플레이어 대쉬로만 파괴 가능
        hp = new CharacterStat(BOX_GIMMICK_HP, 0);
        defense = new CharacterStat(10);
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
        // 자신이 확실히 파괴될 만큼 데미지를 입힘
        (this as IDestructible).HandleHPDecrease(BOX_GIMMICK_HP);
    }

    CharacterStat IDestructible.GetHPStat()
    {
        return hp;
    }

    CharacterStat IDestructible.GetDefenseStat()
    {
        return defense;
    }

    Team IDestructible.GetTeam()
    {
        return Team.Environment;
    }

    bool IDestructible.OnDamage(float finalDamage, StaggerInfo staggerInfo)
    {
        // 박스 기믹은 공격으로 파괴되지 않지만 챕터1 보스전에서
        // 선반 위에 대기 중인 경우 플레이어의 공격에 의해 밀려나며 바닥으로 떨어질 수는 있음.
        //
        // 이 경우 넉백 효과와 함께 부모 오브젝트와의 충돌을 비활성화해주는 처리가 필요함
        if (transform.parent != null && transform.parent.TryGetComponent(out Collider2D shelfCollider))
        {
            DropBoxFromBossRoomShelf(shelfCollider);
            PushBoxHorizontally(staggerInfo.direction);
        }

        // 공격 성공으로는 취급하지 않는다.
        // 플레이어가 상자만 마구 때려서 기어 게이지를 채울 수 있기 때문...
        return false;
    }

    private void DropBoxFromBossRoomShelf(Collider2D shelfCollider)
    {
        transform.parent = null;
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), shelfCollider);
    }

    private void PushBoxHorizontally(Vector2 direction)
    {
        // Note: 플레이어에 의해 밀려나지 않기 위해 mass가 상당히 높게 설정됨!
        rb.AddForce(direction * pushVelocity * rb.mass, ForceMode2D.Impulse);
    }

    void IDestructible.OnDestruction()
    {
        if (shouldDropCreditOnDestroy)
        {
            // TODO: 크레딧이나 아이템 드랍
        }
        // TODO: 파괴 이펙트 재생
        Destroy(gameObject);
    }
}
