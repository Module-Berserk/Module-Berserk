using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(ScreenShake))]
[RequireComponent(typeof(ObjectExistenceSceneState))]
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
    private BoxCollider2D boxCollider;
    private ScreenShake screenShake;
    private Animator animator;

    // 플레이어의 공격에 밀려나게 만들기 위한 IDestructible 요구사항.
    // 방어력에 의한 데미지 감소를 100%로 설정되기 때문에 플레이어가 공격으로 파괴할 수는 없다.
    private CharacterStat hp;
    private CharacterStat defense;

    private const float BOX_GIMMICK_HP = 123456789; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        screenShake = GetComponent<ScreenShake>();
        animator = GetComponent<Animator>();

        // IDestructible 인터페이스에 필요해서 만들기는 하지만
        // 실제로 데미지 처리를 하지는 않으므로 수치는 중요하지 않음.
        // Note: 박스 기믹은 보스의 박스 제거 공격 또는 플레이어 대쉬로만 파괴 가능
        hp = new CharacterStat(BOX_GIMMICK_HP, 0);
        defense = new CharacterStat(10);

        ChooseRandomImage();
    }

    // 박스 이미지 2종 중에서 하나를 랜덤하게 선택!
    private void ChooseRandomImage()
    {
        if (Random.Range(0f, 1f) < 0.5f)
        {
            animator.SetTrigger("UseAlternative");
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // case 1) 상자가 머리 위로 떨어지는 경우 즉시 파괴 (같은 상자나 static rigidbody를 제외한 나머지 모든 물체에 반응)
        bool isGroundCollision = other.rigidbody.bodyType == RigidbodyType2D.Static;
        bool isFalling = rb.velocity.y < -0.3f;
        if (!isGroundCollision && isFalling && other.gameObject.GetComponent<C1BoxGimmick>() == null)
        {
            int[] boxIndices = {33};
            AudioManager.instance.PlaySFXBasedOnPlayer(boxIndices, this.transform);  
            DestroyBox();
        }
        // case 2) 땅에 떨어지면 화면 흔들림
        else if (isGroundCollision)
        {
            int[] boxIndices = {34};
            AudioManager.instance.PlaySFXBasedOnPlayer(boxIndices, this.transform); 
            screenShake.ApplyScreenShake(cameraShakeForce, 0.2f);
        }
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        bool isCollisionHorizontal = Mathf.Approximately(Vector2.Dot(other.GetContact(0).normal, Vector2.up), 0f);
        if (other.gameObject.CompareTag("Player") && isCollisionHorizontal)
        {
            var playerManager = other.gameObject.GetComponent<PlayerManager>();
            Assert.IsNotNull(playerManager);

            // 플레이어가 대쉬로 충돌한 경우 상자 파괴
            if (playerManager.ActionState == PlayerActionState.Evade && playerManager.IsNormalEvasion)
            {
                int[] boxIndices = {33};
                AudioManager.instance.PlaySFXBasedOnPlayer(boxIndices, this.transform);  
                DestroyBox();

                // 1. 경직 상태로 전환
                playerManager.ApplyStunForDurationAsync(playerStunDurationOnDashImpact).Forget();

                // 2. 살짝 튕겨나오는 효과
                float reboundDirection = playerManager.IsFacingLeft ? 0.2f : -0.2f;
                playerManager.ApplyWallRebound(reboundDirection, 0.1f);

                // 3. 화면 흔들림
                screenShake.ApplyScreenShake(cameraShakeForce, 0.2f);
            }
        }
    }

    public void DestroyBox()
    {
        // 자신이 확실히 파괴될 만큼 데미지를 입힘
        (this as IDestructible).HandleHPDecrease(BOX_GIMMICK_HP);

        // 보스전 선반에서 무한 생성되는 상자가 아니라
        // 맵에 원래부터 존재하는 상자인 경우는 세이브 데이터에 파괴되었다고 기록
        var saveState = GetComponent<ObjectExistenceSceneState>();
        if (saveState.ID != "")
        {
            GetComponent<ObjectExistenceSceneState>().RecordAsDestroyed();
        }
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

    bool IDestructible.OnDamage(AttackInfo attackInfo)
    {
        // 박스 기믹은 공격으로 파괴되지 않지만 챕터1 보스전에서
        // 선반 위에 대기 중인 경우 플레이어의 공격에 의해 밀려나며 바닥으로 떨어질 수는 있음.
        //
        // 이 경우 넉백 효과와 함께 부모 오브젝트와의 충돌을 비활성화해주는 처리가 필요함
        if (attackInfo.damageSource == Team.Player && transform.parent != null && transform.parent.TryGetComponent(out Collider2D shelfCollider))
        {
            DropBoxFromBossRoomShelf(shelfCollider);
            PushBoxHorizontally(attackInfo.knockbackForce.normalized);
        }

        // 공격 성공으로는 취급하지 않는다.
        // 플레이어가 상자만 마구 때려서 기어 게이지를 채울 수 있기 때문...
        return false;
    }

    private void DropBoxFromBossRoomShelf(Collider2D shelfCollider)
    {
        transform.parent = null;
        Physics2D.IgnoreCollision(GetComponent<Collider2D>(), shelfCollider);

        animator.SetTrigger("Roll");
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

        // 제자리에 멈추고 충돌 판정 중지
        rb.gravityScale = 0;
        boxCollider.enabled = false;

        // 파괴 애니메이션 재생
        animator.SetTrigger("Break");
    }

    // 파괴 애니메이션의 마지막 프레임에서 호출되는 이벤트.
    public void OnBreakAnimationEnd()
    {
        Destroy(gameObject);
    }
}
