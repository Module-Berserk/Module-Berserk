using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlatformerMovement))]
public class DefaultEnemyStaggerBehavior : MonoBehaviour, IEnemyStaggerBehavior
{
    [Header("Animation Event")]
    [SerializeField] private string staggerAnimationTriggerName = "Stagger";

    [Header("Super Armor")]
    // 슈퍼아머 on/off 상태에서 적용할 경직 저항력.
    // 잡몹은 none <-> weak를 사용하지만 보스는 weak <-> strong를 사용한다.
    [SerializeField] private StaggerStrength defaultStaggerResistance = StaggerStrength.None;
    [SerializeField] private StaggerStrength superArmorStaggerResistance = StaggerStrength.Weak;

    private Animator animator;
    private PlatformerMovement platformerMovement;
    private StaggerStrength staggerResistence = StaggerStrength.None;
    private CancellationTokenSource staggerCancellation = new();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        platformerMovement = GetComponent<PlatformerMovement>();

        // 초기에는 슈퍼아머 없는 상태로 시작함!
        DisableSuperArmor();
    }

    private void FixedUpdate()
    {
        // 넉백 효과 부드럽게 감소
        if ((this as IEnemyStaggerBehavior).IsStaggered)
        {
            platformerMovement.UpdateMoveVelocity(0f);
        }
    }

    bool IEnemyStaggerBehavior.IsStaggered { get; set; } = false;

    bool IEnemyStaggerBehavior.TryApplyStagger(AttackInfo attackInfo)
    {
        if (staggerResistence >= attackInfo.staggerStrength)
        {
            return false;
        }

        if ((this as IEnemyStaggerBehavior).IsStaggered)
        {
            CancelCurrentStaggerEffect();
        }

        GetStaggeredForDuration(attackInfo).Forget();

        return true;
    }

    private void CancelCurrentStaggerEffect()
    {
        staggerCancellation.Cancel();
        staggerCancellation.Dispose();
        staggerCancellation = new();
    }
    
    protected async UniTask GetStaggeredForDuration(AttackInfo attackInfo)
    {
        // 넉백 효과
        platformerMovement.ApplyZeroFriction();
        platformerMovement.UpdateMoveVelocity(attackInfo.knockbackForce.x, skipAcceleration: true);

        animator.SetTrigger(staggerAnimationTriggerName);

        // 보통 애니메이션 이벤트로 슈퍼아머를 켰다 끄는데
        // 경직이 걸리면 경직 모션으로 즉시 전환되므로
        // 슈퍼아머를 해제하는 이벤트가 발생하지 않을 가능성이 있음!
        // ex) Weak 슈퍼아머를 가진 상태에서 충격파에 맞은 경우
        //
        // 그러니 경직 판정이 떴으면 슈퍼아머가
        // 해제되어야 한다고 가정하는 것이 안전함.
        DisableSuperArmor();

        (this as IEnemyStaggerBehavior).IsStaggered = true;

        // 기존 경직이 끝나기 전에 새로운 경직 효과가 부여되는 경우
        // isStaggered를 true 상태로 유지한 채 바로 종료해야 함.
        await UniTask.WaitForSeconds(attackInfo.duration, cancellationToken: staggerCancellation.Token);

        (this as IEnemyStaggerBehavior).IsStaggered = false;
    }

    // 일부 애니메이션에서 경직 저항을 일시적으로
    // 부여하기 위해 애니메이션 이벤트로 호출하는 함수들
    public void EnableSuperArmor()
    {
        staggerResistence = superArmorStaggerResistance;
    }

    public void DisableSuperArmor()
    {
        staggerResistence = defaultStaggerResistance;
    }
}
