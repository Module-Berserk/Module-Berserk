using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;


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


    [Header("Enemy Spawn Pattern")]
    [SerializeField] private GameObject tonpaMobPrefab;
    [SerializeField] private GameObject gunnerMobPrefab;
    // 보스방 왼쪽 문 안쪽에서 소환하면 됨.
    [SerializeField] private Transform mobSpawnPoint;


    [Header("Melee Attack Pattern")]
    [SerializeField] private ApplyDamageOnContact meleeAttackHitbox;


    [Header("Chase Pattern")]
    [SerializeField] private float walkSpeed = 1f;
    // 플레이어가 이 거리보다 가까우면 추적 상태에서도 그냥 idle 모션으로 서있음
    [SerializeField] private float chaseStopDistance = 0.5f;


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
    [SerializeField] private float backstepJumpMaxImpulse = 9f;


    [Header("Dash Attack Pattern")]
    // 맵 반대편 끝까지 돌진하는데에 걸리는 시간
    [SerializeField] private float dashDuration;
    [SerializeField] private Ease dashMotionEase;
    // 돌진이 벽이나 상자에 충돌했을 때의 카메라 흔들림 강도
    [SerializeField] private float dashImpactCameraShakeForce;


    [Header("Cannon Pattern")]
    [SerializeField] private GameObject cannonExplodePrefab;


    [Header("Box Gimmick")]
    // 돌진 패턴이 벽에 충돌하며 끝나는 경우 상자 기믹을 리필함
    [SerializeField] private List<C1BoxGimmickGenerator> boxGenerators;


    [Header("Health Bar UI")]
    [SerializeField] private Slider healthUISlider;


    [Header("Boss Defeat Cutscene")]
    [SerializeField] private PlayableDirector bossDefeatCutscene;

    // 패턴별 쿨타임 등 상수
    // TODO: 기획에 맞게 수치 조정할 것
    private const float BACKSTEP_PATTERN_COOLTIME = 2f;
    private const float MELEE_ATTACK_PATTERN_COOLTIME = 2f;
    private const float BOX_GIMMICK_STUN_DURATION = 3f;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private SpriteRootMotion spriteRootMotion;
    private FlashEffectOnHit flashEffectOnHit;
    private CinemachineImpulseSource cameraShake;
    private GameObject player;

    private GroundContact groundContact;

    private CharacterStat hp;
    private CharacterStat defense;

    private float backstepPatternCooltime = 0f;
    private float meleeAttackPatternCooltime = 0f;
    private bool isDashMotionOngoing = false;
    private bool isCannonShotOngoing = false;

    // 2페이즈가 시작될 때 한 번 시전되는 잡몹 소환 패턴이 이미 끝났는지 기록하는 플래그.
    private bool isEnemySpawnPatternDone = false;

    // 돌진 도중에 박스 기믹과 충돌한 경우 돌진을 멈추고 기절 상태에 진입.
    // 보스가 죽는 경우에도 진행중인 패턴을 모두 종료할 때 사용함.
    private CancellationTokenSource attackCancellation = new();

    // 보스는 기본적으로 약한 경직 저항을 가짐
    private StaggerStrength staggerResistance = StaggerStrength.Weak;

    public enum ActionState
    {
        Chase, // 천천히 플레이어에게 접근
        Stun, // 박스 기믹으로 인한 그로기 상태
        DestroyBox, // 돌진 패턴이 아닌데 상자 기믹과 접촉한 경우 상자를 파괴함 (미리 깔아놓는 것 방지)
        MeleeAttack, // 3연타 근접 공격
        Backstep, // 포격 또는 돌진 패턴으로 이어지는 전조 동작
        BombardAttack, // 3회 포격
        DashAttack, // 맵 끝에서 끝까지 돌진 (상자 기믹과 충돌하면 기절)
        ReinforceMobs, // 2페이즈 돌입할 때 백스텝 후 잡몹 소환
    }
    private ActionState actionState = ActionState.Chase;
    
    public bool IsFacingLeft
    {
        get => spriteRenderer.flipX;
        protected set => spriteRenderer.flipX = value;
    }

    // 2페이즈는 체력 50%부터 시작
    public bool IsSecondPhase { get => hp.CurrentValue < hp.MaxValue * 0.5f; }

    
    // 백스텝 패턴에서 2페이즈에 나오는 연계 공격 종류.
    // 50% 확률로 둘 중 하나를 골라 사용한다.
    private enum ChainAttack
    {
        None, // 1페이즈에서는 연계 x
        CannonShotBeforeDash, // 3발 포격 후 돌진
        MeleeAttackAfterDash, // 돌진 후 즉시 근거리 공격 3연타
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRootMotion = GetComponent<SpriteRootMotion>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
        cameraShake = GetComponent<CinemachineImpulseSource>();
        player = GameObject.FindGameObjectWithTag("Player");

        groundContact = new GroundContact(rb, boxCollider, groundLayer, 0.02f, 0.02f);

        hp = new CharacterStat(501f, 0f, 1000f);
        defense = new CharacterStat(10f, 0f);

        // 체력바 업데이트 콜백
        hp.OnValueChange.AddListener((damage) => healthUISlider.value = hp.CurrentValue / hp.MaxValue);
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
                attackCancellation.Cancel();
                attackCancellation.Dispose();
                attackCancellation = new();
                rb.DOKill();

                // 상자 파괴 & 화면 진동
                boxGimmick.DestroyBox();
                cameraShake.GenerateImpulse(dashImpactCameraShakeForce);

                // 기절 상태 부여
                ApplyBoxGimmickKnockdownAsync().Forget();

                // 쿨타임 처리를 해줄 기존 task가 취소되었으니 여기서 쿨타임 설정을 해줘야 함
                RestartBackstepPatternCooltime();
            }
        }
    }

    // 플레이어가 돌진 패턴에 박스 기믹을 성공적으로 사용하여
    // 보스가 벽이 아니라 박스에 먼저 충돌하면 잠깐 기절 상태에 들어가며
    // 플레이어의 공격에 더 높은 데미지를 입게 됨
    private async UniTask ApplyBoxGimmickKnockdownAsync()
    {
        // 일단 돌진하던 움직임을 멈추고
        rb.velocity = Vector2.zero;

        // 잠시동안 방어력을 반으로 깎으며 기절 상태에 돌입
        actionState = ActionState.Stun;
        animator.SetTrigger("Stun");
        defense.ApplyMultiplicativeModifier(0.5f);

        await UniTask.WaitForSeconds(BOX_GIMMICK_STUN_DURATION, cancellationToken: attackCancellation.Token);

        // 기절이 끝나면 방어력 원상복구
        defense.ApplyMultiplicativeModifier(2f);
        actionState = ActionState.Chase;
    }

    private void FixedUpdate()
    {
        groundContact.TestContact();

        UpdatePatternCooltimes();

        if (actionState == ActionState.Stun)
        {
            // 기절 상태에서는 아무것도 하지 않음
        }
        else if (actionState == ActionState.Backstep && groundContact.IsGrounded)
        {
            // 백스텝 점프 후 착지하면 미끄러지지 않고 제자리에 멈추도록 함
            rb.velocity = Vector2.zero;
        }
        else if (actionState == ActionState.MeleeAttack)
        {
            // 근접 공격 3연타는 움직임이 복잡해서 루트 모션으로 처리함
            spriteRootMotion.ApplyVelocity(IsFacingLeft);
        }
        else if (actionState == ActionState.Chase)
        {
            // 플레이어 방향 바라보기
            LookAtPlayer();

            if (!isEnemySpawnPatternDone && IsSecondPhase)
            {
                PerformEnemySpawnPatternAsync().Forget();
            }
            else if (!backstepRange.IsPlayerInRange && backstepPatternCooltime <= 0f)
            {
                PerformBackstepPatternAsync().Forget();
            }
            else if (meleeAttackRange.IsPlayerInRange && meleeAttackPatternCooltime <= 0f)
            {
                PerformMeleeAttackPatternAsync().Forget();
            }
            else
            {
                WalkTowardsPlayer();
            }
        }

        UpdateAnimatorState();
    }

    public void LookAtPlayer()
    {
        IsFacingLeft = player.transform.position.x < rb.position.x;
        meleeAttackRange.SetDetectorDirection(IsFacingLeft);
        meleeAttackHitbox.SetHitboxDirection(IsFacingLeft);
    }

    private async UniTask PerformEnemySpawnPatternAsync()
    {
        // 잡몹 소환은 2페이즈 시작할 때 한 번만 해야하니 이미 끝났다고 기록
        isEnemySpawnPatternDone = true;

        actionState = ActionState.ReinforceMobs;

        // 일단 맵 오른쪽 끝으로 백스텝 점프
        IsFacingLeft = true;
        float jumpDuration = ApplyBackstepJumpForce(mapRightEnd.position.x);
        await UniTask.WaitForSeconds(jumpDuration);

        // 잡몹 소환 애니메이션 재생
        // TODO: 아트 완성되면 애니메이터에서 c1_boss_spawn_enemy_temp 클립 교체하기
        animator.SetTrigger("SpawnEnemy");

        // 준비 동작
        await UniTask.WaitForSeconds(1f, cancellationToken: attackCancellation.Token);

        const int numTonpaMob = 2;
        const int numGunnerMob = 1;

        // 생성하고 잠깐 기다렸다가 플레이어를 감지하게 하는 이유:
        // spawnPoint가 정확하지 않으면 spawn 직후에 IsGrounded가 false가 나와버려
        // 잡몹이 즉시 추적을 중지하고 patrol 상태로 돌입해버림!
        // rigidbody가 지면 위에 안정적으로 올라갈 때까지 기다려줘야 플레이어를 잘 따라온다.
        for (int i = 0; i < numTonpaMob; ++i)
        {
            var tonpaMob = Instantiate(tonpaMobPrefab, mobSpawnPoint.position, Quaternion.identity);
            await UniTask.WaitForSeconds(0.2f, cancellationToken: attackCancellation.Token);
            tonpaMob.GetComponent<MeleeEnemyController>().HandlePlayerDetection();
        }

        for (int i = 0; i < numGunnerMob; ++i)
        {
            var gunnerMob = Instantiate(gunnerMobPrefab, mobSpawnPoint.position, Quaternion.identity);
            await UniTask.WaitForSeconds(0.2f, cancellationToken: attackCancellation.Token);
            gunnerMob.GetComponent<RangedEnemyController>().HandlePlayerDetection();
        }
    }

    // 적 소환 모션 끝나면 애니메이션에서 이벤트로 상태 변경해줌
    public void OnSpawnEnemyMotionEnd()
    {
        actionState = ActionState.Chase;
    }

    private async UniTask PerformMeleeAttackPatternAsync()
    {
        animator.SetTrigger("MeleeAttack");
        actionState = ActionState.MeleeAttack;
        spriteRootMotion.HandleAnimationChange();

        // 공격 끝날 때까지 기다리기.
        // 애니메이션 끝나면 OnMeleeAttackMotionEnd()에서 값 설정해줌.
        await UniTask.WaitUntil(() => actionState == ActionState.Chase);
    }

    public void OnMeleeAttackMotionEnd()
    {
        actionState = ActionState.Chase;
        meleeAttackPatternCooltime = MELEE_ATTACK_PATTERN_COOLTIME;
    }

    private void UpdatePatternCooltimes()
    {
        backstepPatternCooltime -= Time.fixedDeltaTime;
        meleeAttackPatternCooltime -= Time.fixedDeltaTime;
    }

    private void WalkTowardsPlayer()
    {
        // 플레이어가 너무 가깝지 않은 경우에만 걸어서 다가감.
        // 이 조건이 없으면 플레이어와 정확히 겹쳐서 좌우로 왔다갔다하는 이상한 모습이 연출됨...
        float distanceToPlayer = Mathf.Abs(player.transform.position.x - rb.position.x);
        if (distanceToPlayer > chaseStopDistance)
        {
            // Note: 걷기 상태인 경우 FixedUpdate에서 먼저 플레이어를 바라보게 IsFacingLeft 값을 설정해줌
            rb.velocity = (IsFacingLeft ? Vector2.left : Vector2.right) * walkSpeed;
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    private async UniTask PerformBackstepPatternAsync()
    {
        actionState = ActionState.Backstep;
        animator.SetTrigger("Backstep");

        // 플레이어를 등지는 방향으로 맵 끝까지 점프
        float jumpTargetX = GetBackstepJumpTargetX();
        float jumpDuration = ApplyBackstepJumpForce(jumpTargetX);

        // 백스텝 모션 끝날 때까지 기다리기...
        // 점프하는 동안은 위에 대기중인 박스 기믹 등과
        // 충돌하면 안되므로 일단 콜라이더를 비활성화하고
        // 착지 직전에 다시 활성화하는 방식으로 처리함!
        boxCollider.enabled = false;
        await UniTask.WaitForSeconds(jumpDuration * 0.8f, cancellationToken: attackCancellation.Token);
        boxCollider.enabled = true;
        await UniTask.WaitForSeconds(jumpDuration * 0.2f, cancellationToken: attackCancellation.Token);

        // TODO: 맵에 떨어진 상자가 남아있다면 무조건 포격 패턴만 사용
        // 그게 아니라면 반반 확률로 포격 또는 돌진 패턴 사용
        if (Random.Range(0f, 1f) < 0.5f)
        {
            // 1페이즈에서는 그냥 백스텝->돌진만 하고 끝나지만
            // 2페이즈에서는 3연속 포격 패턴과 돌진 패턴 중에서 하나가 랜덤하게 연계됨
            var chainAttack = ChainAttack.None;
            if (IsSecondPhase)
            {
                chainAttack = Random.Range(0f, 1f) < 0.5f ? ChainAttack.CannonShotBeforeDash : ChainAttack.MeleeAttackAfterDash;
            }

            if (chainAttack == ChainAttack.CannonShotBeforeDash)
            {
                await PerformShortCannonPatternAsync();
            }

            await PerformDashAttackPatternAsync();

            if (chainAttack == ChainAttack.MeleeAttackAfterDash)
            {
                await PerformMeleeAttackPatternAsync();
            }
        }
        else
        {
            await PerformLongCannonPatternAsync();
        }

        // 백스텝 패턴 쿨타임 부여하고 기본 상태로 복귀
        // TODO: 쿨타임 수치는 기획에 따라 바꿀 것
        RestartBackstepPatternCooltime();
        actionState = ActionState.Chase;
    }

    // 한 번의 점프로 x축 좌표를 목적지까지 움직일 수 있게
    // rigidbody를 밀고 목적지까지의 거리에 비례한 체공 시간을 반환함
    private float ApplyBackstepJumpForce(float jumpTargetX)
    {
        float jumpDistance = Mathf.Abs(rb.position.x - jumpTargetX);
        float maxJumpDistance = Mathf.Abs(mapRightEnd.position.x - mapLeftEnd.position.x);
        float jumpDistanceRatio = jumpDistance / maxJumpDistance; // 맵 끝에서 끝까지 점프하는걸 1로 잡았을 때의 점프 거리
        jumpDistanceRatio = Mathf.Max(0.4f, jumpDistanceRatio); // 이미 끝에 가까운 경우에도 살짝은 점프하도록 최소치를 부여함

        float jumpDuration = backstepJumpMaxDuration * jumpDistanceRatio;

        float velocityX = (jumpTargetX - rb.position.x) / jumpDuration;
        float velocityY = backstepJumpMaxImpulse * jumpDistanceRatio;
        rb.velocity = new Vector2(velocityX, velocityY);
        
        return jumpDuration;
    }

    private void RestartBackstepPatternCooltime()
    {
        backstepPatternCooltime = BACKSTEP_PATTERN_COOLTIME;
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

    private async UniTask PerformLongCannonPatternAsync()
    {
        animator.SetTrigger("LongCannonShot");

        // 준비 동작 끝나면 애니메이션에서 이벤트로 설정해줌
        isCannonShotOngoing = false;
        await UniTask.WaitUntil(() => isCannonShotOngoing, cancellationToken: attackCancellation.Token);

        // 8발의 포탄을 순차적으로 보스 앞에서부터 떨어트림
        List<float> explodePositions = new();
        for (int i = 0; i < 8; ++i)
        {
            const float offsetFromBoss = 1f; // 보스 앞으로 얼마나 멀리서 떨어지기 시작할 것인지
            const float distanceBetweenExplosions = 1.5f; // 포탄 사이의 간격
            float relativePosition = (IsFacingLeft ? -1f : 1f) * (distanceBetweenExplosions * i + offsetFromBoss);
            explodePositions.Add(rb.position.x + relativePosition);
        }

        // 절반의 확률로 떨어지는 순서가 반대로 바뀜
        if (Random.Range(0f, 1f) < 0.5f)
        {
            explodePositions.Reverse();
        }

        foreach (float explodePosition in explodePositions)
        {
            Vector3 spawnPosition = new Vector3(explodePosition, transform.position.y);
            Instantiate(cannonExplodePrefab, spawnPosition, Quaternion.identity);
            
            await UniTask.WaitForSeconds(0.2f, cancellationToken: attackCancellation.Token);
        }

        // 모션 다 끝나면 애니메이션에서 이벤트로 설정해줌
        await UniTask.WaitUntil(() => !isCannonShotOngoing);
    }

    private async UniTask PerformShortCannonPatternAsync()
    {
        animator.SetTrigger("ShortCannonShot");

        // 준비 동작 끝나면 애니메이션에서 이벤트로 설정해줌
        isCannonShotOngoing = false;
        await UniTask.WaitUntil(() => isCannonShotOngoing, cancellationToken: attackCancellation.Token);

        // 맵을 벗어나지 않는 선에서 플레이어 중심, 왼쪽, 오른쪽에 하나씩 생성
        float playerX = player.transform.position.x;
        List<float> explodePositions = new()
        {
            playerX,
            Mathf.Clamp(playerX + Random.Range(1.5f, 3f), mapLeftEnd.position.x, mapRightEnd.position.x),
            Mathf.Clamp(playerX - Random.Range(1.5f, 3f), mapLeftEnd.position.x, mapRightEnd.position.x),
        };

        for (int i = 0; i < 3; ++ i)
        {
            Vector3 spawnPosition = new Vector3(explodePositions[i], transform.position.y);
            Instantiate(cannonExplodePrefab, spawnPosition, Quaternion.identity);
            
            await UniTask.WaitForSeconds(0.2f, cancellationToken: attackCancellation.Token);
        }

        // 모션 다 끝나면 애니메이션에서 이벤트로 설정해줌
        await UniTask.WaitUntil(() => !isCannonShotOngoing, cancellationToken: attackCancellation.Token);
    }

    // 애니메이션에서 실제 포격 시작과 끝에 호출해주는 이벤트.
    // UniTask.WaitUntil()에서 대기 조건으로 사용된다.
    public void BeginCannonShot()
    {
        isCannonShotOngoing = true;
    }

    public void EndCannonShot()
    {
        isCannonShotOngoing = false;
    }

    private async UniTask PerformDashAttackPatternAsync()
    {
        actionState = ActionState.DashAttack;

        animator.SetTrigger("DashAttack");
        spriteRootMotion.HandleAnimationChange();

        // 애니메이션 이벤트에 의해 돌진이 시작되기를 기다림
        isDashMotionOngoing = false;
        await UniTask.WaitUntil(() => isDashMotionOngoing, cancellationToken: attackCancellation.Token);

        // 박스 기믹과 충돌하는 경우를 대비해 cancellation token을 넣어준다
        await UniTask.WaitForSeconds(dashDuration, cancellationToken: attackCancellation.Token);

        // task cancellation 없이 이 라인에 도달했다는건
        // 박스에 부딛히지 않고 벽에 충돌했다는 뜻이므로
        // 약간의 화면 흔들림과 함께 박스를 리필해줌
        cameraShake.GenerateImpulse(dashImpactCameraShakeForce);
        foreach (var boxGenerator in boxGenerators)
        {
            boxGenerator.TryGenerateNewBox();
        }

        // 모션 다 끝나면 애니메이션에서 이벤트로 설정해줌
        await UniTask.WaitUntil(() => !isDashMotionOngoing, cancellationToken: attackCancellation.Token);
    }

    // 돌진 공격 애니메이션에서 앞으로 튀어나가야 하는 순간에 호출해주는 이벤트.
    public void BeginDashAttackMovement()
    {
        float dashTargetX = GetDashTargetX();
        rb.DOMoveX(dashTargetX, dashDuration).SetEase(dashMotionEase);
        isDashMotionOngoing = true;
    }

    // 돌진 패턴의 UniTask를 애니메이션이 종료될 때까지 기다리도록 만드는 데에 사용됨.
    public void EndDashAttackMovement()
    {
        isDashMotionOngoing = false;
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
        animator.SetBool("IsStunned", actionState == ActionState.Stun);
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

    bool IDestructible.OnDamage(Team damageSource, float finalDamage, StaggerInfo staggerInfo)
    {
        if (staggerInfo.strength > staggerResistance)
        {
            // TODO: 경직 부여
            Debug.Log("보스 경직");
        }

        (this as IDestructible).HandleHPDecrease(finalDamage);

        flashEffectOnHit.StartEffectAsync().Forget();

        // 챕터1 보스는 무적 시간 없음
        return true;
    }

    void IDestructible.OnDestruction()
    {
        // 진행중인 행동 모두 종료
        attackCancellation.Cancel();
        rb.velocity = Vector2.zero;

        // TODO: 아트 완성되면 스턴 대신 제대로된 패배 애니메이션 재생
        actionState = ActionState.Stun;
        animator.SetTrigger("Stun");

        // 챕터1 보스전 종료 컷신 시작하기 (살리기 vs 죽이기 선택)
        bossDefeatCutscene.Play();

        // 삭제할 오브젝트에 tweening이 살아있을 수 있으니 오류 방지를 위해 모두 종료
        DOTween.KillAll();

        // 남아있는 잡몹은 컷신에 방해되니까 다 없애기
        foreach(var meleeEnemy in FindObjectsByType<MeleeEnemyController>(FindObjectsSortMode.None))
        {
            Destroy(meleeEnemy.gameObject);
        }
        foreach(var rangedEnemy in FindObjectsByType<RangedEnemyController>(FindObjectsSortMode.None))
        {
            Destroy(rangedEnemy.gameObject);
        }

        // 보스전이 끝났으니 더이상 AI가 조종하지 못하도록 막기
        enabled = false;
    }

    //SFX
    private void PlayCannonShotSFX() {
        int[] cannonIndices = {21};
        AudioManager.instance.PlaySFX(cannonIndices);  
    }
    private void PlayJumpingSFX() {
        int[] jumpingIndices = {18};
        AudioManager.instance.PlaySFX(jumpingIndices);        
    }
    private void PlayLandSFX() {
        int[] landingIndices = {9};
        AudioManager.instance.PlaySFX(landingIndices);        
    }
    private void PlayChargeSFX() {
        int[] chargingIndices = {22};
        AudioManager.instance.PlaySFX(chargingIndices);        
    }
    private void PlayCrashSFX() {
        int[] crashingIndices = {23, 24};
        AudioManager.instance.PlaySFX(crashingIndices);        
    }
    private void PlayPunchSFX() {
        int[] punchIndices = {25, 26, 27, 28};
        AudioManager.instance.PlaySFX(punchIndices);        
    }
}
