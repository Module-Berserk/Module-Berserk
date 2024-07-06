using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;

// 챕터1 보스의 AI로 스크립트가 활성화된 동안 정해진 패턴대로 움직이게 만든다.
// 컷신 도중에는 이 스크립트를 비활성화해줘야 한다.
//
// 잡몹들과 다르게 보스는 공유하는 특징이
// 거의 없어서 EnemyBehavior 등을 사용하지 않는다.
public class C1BossController : MonoBehaviour, IDestructible
{
    [Header("Player Detectors")]
    // 플레이어가 이 범위 안에 들어오면 3연타 근접 공격을 시도함
    [SerializeField] private PlayerDetectionRange meleeAttackRange;
    // 플레이어가 이 범위 밖으로 나가면 맵 끝으로 백스텝한 뒤 후속 패턴을 시전함
    [SerializeField] private PlayerDetectionRange backstepRange;


    [Header("Backstep Pattern")]
    // 점프 후 착지 여부를 판단할 때 사용
    [SerializeField] private LayerMask groundLayer;
    // 백스텝 패턴 점프 목적지(맵의 양 끝 지점)의 x 좌표를 얻기 위해 참조
    [SerializeField] private Transform mapLeftEnd;
    [SerializeField] private Transform mapRightEnd;
    // 맵 끝에서 끝까지 점프할 때를 기준으로 점프 모션이
    // 몇 초 안에, 그리고 위로 얼마나 빠르게 솟아오르는가?
    //
    // 실제로는 백스텝 거리에 비례하는 수치가 사용됨
    // ex) 맵 중앙에서 출발하면 시간과 속도가 절반
    [SerializeField] private float backstepJumpMaxDuration = 2f;
    [SerializeField] private float backstepJumpMaxImpulse = 20f;


    [Header("Dash Attack Pattern")]
    // 맵 반대편 끝까지 돌진하는데에 걸리는 시간
    [SerializeField] private float dashDuration;
    [SerializeField] private Ease dashMotionEase;
    // 돌진이 벽이나 상자에 충돌했을 때의 카메라 흔들림 강도
    [SerializeField] private float dashImpactCameraShakeForce;


    [Header("Box Gimmick")]
    // 돌진 패턴이 벽에 충돌하며 끝나는 경우 상자 기믹을 리필함
    [SerializeField] private List<C1BoxGimmickGenerator> boxGenerators;


    [Header("Boss Defeat Cutscene")]
    [SerializeField] private PlayableDirector bossDefeatCutscene;


    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private SpriteRootMotion spriteRootMotion;
    private CinemachineImpulseSource cameraShake;
    private GameObject player;

    private GroundContact groundContact;

    private CharacterStat hp;
    private CharacterStat defense;

    private float backstepPatternCooltime = 0f;
    private float meleeAttackPatternCooltime = 0f;
    private bool isDashMotionStarted = false;

    // 돌진 도중에 박스 기믹과 충돌한 경우 돌진을 멈추고 기절 상태에 진입
    private CancellationTokenSource dashAttackCancellation = new();

    // 보스는 기본적으로 약한 경직 저항을 가짐
    private StaggerStrength staggerResistance = StaggerStrength.Weak;

    public enum ActionState
    {
        Chase, // 천천히 플레이어에게 접근
        Stagger, // 경직 또는 기절
        DestroyBox, // 돌진 패턴이 아닌데 상자 기믹과 접촉한 경우 상자를 파괴함 (미리 깔아놓는 것 방지)
        MeleeAttack, // 3연타 근접 공격
        Backstep, // 포격 또는 돌진 패턴으로 이어지는 전조 동작
        BombardAttack, // 3회 포격
        DashAttack, // 맵 끝에서 끝까지 돌진 (상자 기믹과 충돌하면 기절)
        ReinforceMobs, // 체력이 25% 감소할 때마다 잡몹 소환
    }
    private ActionState actionState = ActionState.Chase;
    
    public bool IsFacingLeft
    {
        get => spriteRenderer.flipX;
        protected set => spriteRenderer.flipX = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRootMotion = GetComponent<SpriteRootMotion>();
        cameraShake = GetComponent<CinemachineImpulseSource>();
        player = GameObject.FindGameObjectWithTag("Player");

        groundContact = new GroundContact(rb, boxCollider, groundLayer, 0.02f, 0.02f);

        hp = new CharacterStat(100f, 0f, 100f);
        defense = new CharacterStat(10f, 0f);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.TryGetComponent(out C1BoxGimmick boxGimmick))
        {
            // TODO: chase 상태였다면 공격으로 파괴하기

            // 돌진 패턴 도중에 박스와 충돌한 경우 돌진을 멈추고 기절
            if (actionState == ActionState.DashAttack)
            {
                // 돌진 멈추기
                dashAttackCancellation.Cancel();
                dashAttackCancellation.Dispose();
                dashAttackCancellation = new();
                rb.DOKill();

                // 상자 파괴 & 화면 진동
                boxGimmick.DestroyBox();
                cameraShake.GenerateImpulse(dashImpactCameraShakeForce);

                // TODO: 기절 상태 부여
                actionState = ActionState.Chase;
                
                // 쿨타임 처리를 해줄 기존 task가 취소되었으니 여기서 쿨타임 설정을 해줘야 함
                RestartBackstepPatternCooltime();
            }
        }
    }

    private void FixedUpdate()
    {
        // TODO: detection range들과 보스 스프라이트 렌더러 바라보는 방향으로 flipping 처리
        groundContact.TestContact();

        UpdatePatternCooltimes();
        

        if (actionState == ActionState.Backstep && groundContact.IsGrounded)
        {
            // 백스텝 점프 후 착지하면 미끄러지지 않고 제자리에 멈추도록 함
            rb.velocity = Vector2.zero;
        }
        else if (actionState == ActionState.Chase)
        {
            // 플레이어 방향 바라보기
            IsFacingLeft = player.transform.position.x < rb.position.x;

            // TODO: 맨 앞에 체력 25% 지점마다 쓰는 잡몹 소환 패턴 끼워넣기 (이게 제일 우선순위 높음)
            if (!backstepRange.IsPlayerInRange && backstepPatternCooltime <= 0f)
            {
                PerformBackstepPatternAsync().Forget();
            }
            else if (meleeAttackPatternCooltime <= 0f)
            {
                // TODO: 근접공격 패턴 시작
            }
        }

        UpdateAnimatorState();
    }

    private void UpdatePatternCooltimes()
    {
        backstepPatternCooltime -= Time.fixedDeltaTime;
        meleeAttackPatternCooltime -= Time.fixedDeltaTime;
    }

    private async UniTask PerformBackstepPatternAsync()
    {
        actionState = ActionState.Backstep;
        animator.SetTrigger("Backstep");

        // 전조도 없이 점프하면 이상하니까 준비 동작 끝날 때까지 잠깐 기다리기
        // Note: 애니메이션을 받아보니 준비 동작이 없어서 일단 주석처리함
        // await UniTask.WaitForSeconds(1f);

        // 플레이어를 등지는 방향으로 맵 끝까지 점프
        // x축으로는 tweening, y축으로는 점프 거리에 비례한 속도 부여
        float jumpTargetX = GetBackstepJumpTargetX();
        float jumpDistance = Mathf.Abs(rb.position.x - jumpTargetX);
        float maxJumpDistance = Mathf.Abs(mapRightEnd.position.x - mapLeftEnd.position.x);
        float jumpDistanceRatio = jumpDistance / maxJumpDistance; // 맵 끝에서 끝까지 점프하는걸 1로 잡았을 때의 점프 거리
        jumpDistanceRatio = Mathf.Max(0.4f, jumpDistanceRatio); // 이미 끝에 가까운 경우에도 살짝은 점프하도록 최소치를 부여함

        float jumpDuration = backstepJumpMaxDuration * jumpDistanceRatio;
        rb.AddForce(Vector2.up * backstepJumpMaxImpulse * jumpDistanceRatio, ForceMode2D.Impulse);
        rb.AddForce(Vector2.right * (jumpTargetX - rb.position.x) / jumpDuration, ForceMode2D.Impulse);

        // await UniTask.WaitForSeconds(jumpDuration * 0.1f);
        // rb.DOMoveX(jumpTargetX, jumpDuration).SetEase(Ease.OutSine).SetUpdate(UpdateType.Fixed);

        // 백스텝 모션 끝날 때까지 기다리기...
        // 점프하는 동안은 위에 대기중인 박스 기믹 등과
        // 충돌하면 안되므로 일단 콜라이더를 비활성화하고
        // 착지 직전에 다시 활성화하는 방식으로 처리함!
        boxCollider.enabled = false;
        await UniTask.WaitForSeconds(jumpDuration * 0.8f);
        boxCollider.enabled = true;
        await UniTask.WaitForSeconds(jumpDuration * 0.2f);

        // TODO: 맵에 떨어진 상자가 남아있다면 무조건 포격 패턴만 사용
        // 그게 아니라면 반반 확률로 포격 또는 돌진 패턴 사용
        await PerformDashAttackPatternAsync();

        // 백스텝 패턴 쿨타임 부여하고 기본 상태로 복귀
        // TODO: 쿨타임 수치는 기획에 따라 바꿀 것
        RestartBackstepPatternCooltime();
        actionState = ActionState.Chase;
    }

    private void RestartBackstepPatternCooltime()
    {
        backstepPatternCooltime = 3f;
    }

    private float GetBackstepJumpTargetX()
    {
        // 플레이어가 나보다 오른쪽에 있다면 왼쪽으로 점프
        if (player.transform.position.x > transform.position.x)
        {
            return mapLeftEnd.position.x;
        }
        // 플레이어가 나보다 왼쪽에 있다면 오른쪽으로 점프
        else
        {
            return mapRightEnd.position.x;
        }
    }

    private async UniTask PerformDashAttackPatternAsync()
    {
        actionState = ActionState.DashAttack;

        animator.SetTrigger("DashAttack");
        spriteRootMotion.HandleAnimationChange();

        // 애니메이션 이벤트에 의해 돌진이 시작되기를 기다림
        isDashMotionStarted = false;
        await UniTask.WaitUntil(() => isDashMotionStarted);

        // 박스 기믹과 충돌하는 경우를 대비해 cancellation token을 넣어준다
        await UniTask.WaitForSeconds(dashDuration, cancellationToken: dashAttackCancellation.Token);

        // task cancellation 없이 이 라인에 도달했다는건
        // 박스에 부딛히지 않고 벽에 충돌했다는 뜻이므로
        // 약간의 화면 흔들림과 함께 박스를 리필해줌
        cameraShake.GenerateImpulse(dashImpactCameraShakeForce);
        foreach (var boxGenerator in boxGenerators)
        {
            boxGenerator.TryGenerateNewBox();
        }

        // 돌진 패턴 후딜레이
        await UniTask.WaitForSeconds(1f);
    }

    // 돌진 공격 애니메이션에서 앞으로 튀어나가야 하는 순간에 호출해주는 이벤트.
    public void BeginDashAttackMovement()
    {
        float dashTargetX = GetDashTargetX();
        rb.DOMoveX(dashTargetX, dashDuration).SetEase(dashMotionEase);
        isDashMotionStarted = true;
    }

    // 돌진 목적지는 맵의 양쪽 끝 중에서 더 멀리 있는 곳
    private float GetDashTargetX()
    {
        float leftEndDistance = Mathf.Abs(rb.position.x - mapLeftEnd.position.x);
        float rightEndDistance = Mathf.Abs(rb.position.x - mapRightEnd.position.x);
        if (leftEndDistance < rightEndDistance)
        {
            return mapRightEnd.position.x;
        }
        else
        {
            return mapLeftEnd.position.x;
        }
    }

    private void UpdateAnimatorState()
    {
        animator.SetBool("IsWalking", !Mathf.Approximately(rb.velocity.x, 0f));
        animator.SetBool("IsGrounded", groundContact.IsGrounded);
    }

    CharacterStat IDestructible.GetDefenseStat()
    {
        return defense;
    }

    CharacterStat IDestructible.GetHPStat()
    {
        return hp;
    }

    Team IDestructible.GetTeam()
    {
        return Team.Enemy;
    }

    bool IDestructible.OnDamage(float finalDamage, StaggerInfo staggerInfo)
    {
        if (staggerInfo.strength > staggerResistance)
        {
            // TODO: 경직 부여
            Debug.Log("보스 경직");
        }

        (this as IDestructible).HandleHPDecrease(finalDamage);

        // 챕터1 보스는 무적 시간 없음
        return true;
    }

    void IDestructible.OnDestruction()
    {
        // 챕터1 보스전 종료 컷신 시작하기 (살리기 vs 죽이기 선택)
        bossDefeatCutscene.Play();

        // 보스전이 끝났으니 더이상 AI가 조종하지 못하도록 막기
        enabled = false;
    }
}
