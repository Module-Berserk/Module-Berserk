using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public enum PlayerActionState
{
    IdleOrRun, // 서있기, 달리기, 점프, 낙하
    StickToWall, // 벽에 매달려 정지한 상태
    Stagger, // 공격에 맞아 경직 판정인 상태
    AttackInProgress, // 공격 모션의 선딜 ~ 후딜까지의 기간 (선입력 대기하는 중)
    AttackWaitingContinuation, // 선입력은 없었지만 언제든 공격 키를 눌러 다음 공격을 이어나갈 수 있는 상태
    AttackWaitingEnd, // AttackWaitingContinuation에서 더이상 다음 공격이 자연스럽게 이어지지 않는 경우 돌입하는 상태
    Evade, // 회피
    Stun, // 챕터1 박스 기믹 등에 의해 기절당한 상태. Stagger와 마찬가지로 최우선 판정.
};

// 주인공의 이동/공격/상호작용 등 각종 조작을 구현하는 클래스.
//
// 필요한 애니메이션 이벤트:
// 1. OnEnableAttackCollider() 
//    - 선딜레이가 끝나고 공격 판정이 시작되는 시점
// 2. OnBeginAttackInputBuffering()
//    - 선입력을 기록하기 시작할 시점 (1번 이벤트의 다음 프레임)
// 3. OnDisableAttackCollider()
//    - 타격 모션이 끝나고 후딜레이가 시작되는 시점
// 4. OnStartWaitingAttackContinuation()
//    - 선입력에 의해 자동으로 공격을 이어나가는 시점 (복귀 자세 시작되는 프레임)
//    - 연속 공격의 마지막 콤보 뒤에 딜레이를 의도적으로 넣고싶은 경우
//      이 이벤트를 없애서 복귀 자세를 강제하는 방식으로 처리할 수 있음
// 5. OnAttackMotionEnd() - 각 공격 모션의 마지막 프레임
//
// 필요한 애니메이션 컨트롤러 설정:
// - 공격 모션마다 Speed Multiplier로 AttackSpeed 파라미터 할당
//
// 제공되는 애니메이션 트리거:
// 1. Jump
// 2. Evade - 일반 회피
// 3. AttackN - N번째 공격 모션 시작 ex) Attack1, Attack2, ...
//
// 필요한 인스펙터 drag & drop 설정:
// 1. cameraFollowTarget - cinemachine 카메라의 추적 대상으로 설정된 빈 오브젝트
public class PlayerManager : MonoBehaviour, IDestructible
{
    [Header("Follow Camera Target")]
    // Cinemachine Virtual Camera의 Follow로 등록된 오브젝트를 지정해줘야 함!
    // 새로운 높이의 플랫폼에 착지하기 전까지 카메라의 y축 좌표를 일정하게 유지하는 용도.
    [SerializeField] private GameObject cameraFollowTarget;
    // 바라보는 방향으로 얼마나 앞에 있는 지점을 카메라가 추적할 것인지
    [SerializeField, Range(0f, 5f)] private float cameraLookAheadDistance = 2f;


    [Header("Stagger")]
    // 경직을 주는 공격에 맞았을 때 얼마나 강하게 밀려날 것인지
    [SerializeField] private float weakStaggerKnockbackForce = 10f;
    [SerializeField] private float strongStaggerKnockbackForce = 23f;


    [Header("Hitbox")]
    [SerializeField] private ApplyDamageOnContact weaponHitbox; // 평타 범위
    [SerializeField] private ApplyDamageOnContact emergencyEvadeHitbox; // 긴급회피 밀치기 범위


    [Header("Evasion")]
    [SerializeField] private float evasionDuration = 0.4f; // 회피 모션의 재생 시간과 일치해야 자연스러움!
    [SerializeField] private float evasionDistance = 2f;
    [SerializeField] private Ease evasionEase = Ease.OutCubic;
    [SerializeField] private float evasionCooltime = 2f;
    [SerializeField] private float emergencyEvasionCooltime = 1f;
    // 피격 시점 이후로 긴급 회피가 허용되는 시간.
    // 이 시간 안에 회피 버튼을 누르면 데미지를 무효화하고 반격할 수 있음.
    [SerializeField] private float emergencyEvasionTimeWindow = 0.3f;

    public bool IsFacingLeft
    {
        get => spriteRenderer.flipX;
        protected set => spriteRenderer.flipX = value;
    }

    // 컴포넌트 레퍼런스
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlatformerMovement platformerMovement;
    private SpriteRootMotion spriteRootMotion;
    private InteractionManager interactionManager;
    private FlashEffectOnHit flashEffectOnHit;
    private ScreenShake screenShake;
    private GearSystem gearSystem;
    private HealthBarAnimation healthBarAnimation;

    // GameState에서 가져온 저장 가능한 플레이어 상태들
    private PlayerState playerState;

    private bool isJumpKeyPressed = false;
    
    //Prototype 공격용 변수들
    private bool isAttackInputBufferingAllowed = false; // 공격 모션 중에서 선입력 기록이 가능한 시점에 도달했는지
    private bool isAttackInputBuffered = false; // 공격 버튼 선입력 여부
    private bool isAirAttackPerformed = false; // 공중 공격을 이미 했는지 (점프마다 한 번 가능)
    private int attackCount = 0;
    private int maxAttackCount = 4; // 최대 연속 공격 횟수. attackCount가 이보다 커지면 첫 공격 모션으로 돌아감.

    // 무적 판정
    private bool isInvincible = false;
    // 마지막 회피로부터 지난 시간.
    // 이 값이 회피 쿨타임보다 크면 회피 가능.
    // 캐릭터가 생성된 직후에도 회피가 가능하도록 쿨타임보다 확실히 큰 초기값을 부여함.
    private float timeSinceLastEvasion = 10000f;

    // 마지막 긴급회피로부터 지난 시간.
    private float timeSinceLastEmergencyEvasion = 10000f;

    // 경직 도중에 또 경직을 당하거나 긴급 회피로 탈출하는 경우 기존 경직 취소
    private CancellationTokenSource staggerCancellation = new();
    // 기절 상태에서 공격을 받는 경우 기절 취소하고 경직 상태로 전환
    private CancellationTokenSource stunCancellation = new();
    // 긴급 회피를 시전할 때 직전에 입은 데미지를 무효화
    private CancellationTokenSource damageCancellation = new();
    // 긴급 회피가 일어나는지 확인하려고 대기 중인 데미지 목록.
    // 회피 버튼을 눌렀을 때 긴급 회피로 처리해야 하는지 확인하기 위해 사용함.
    private int numPendingDamages = 0;

    // 회피를 할 때마다 해당 회피 타입을 여기에 기록해둔다.
    // 챕터 1 박스 기믹에서 일반 대쉬에만 박스가 파괴되도록 구분하기 위해 사용함.
    public bool IsNormalEvasion {get; private set;}

    public PlayerActionState ActionState {get; private set;} = PlayerActionState.IdleOrRun;

    private void Awake()
    {
        FindComponentReferences();
    }

    private void FindComponentReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        platformerMovement = GetComponent<PlatformerMovement>();
        spriteRootMotion = GetComponent<SpriteRootMotion>();
        interactionManager = GetComponent<InteractionManager>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
        screenShake = GetComponent<ScreenShake>();
        gearSystem = GetComponent<GearSystem>();
        healthBarAnimation = GetComponent<HealthBarAnimation>();
    }

    private void Start()
    {
        InitializePlayerState();

        platformerMovement.OnLand.AddListener(PlayLandSFX);
    }

    // scene 로딩이 끝난 뒤 호출되는 함수.
    // 직전 scene에서의 상태를 복원한다.
    private void InitializePlayerState()
    {
        playerState = GameStateManager.ActiveGameState.PlayerState;

        // 포탈을 타고 다른 scene으로 넘어온 경우 해당 포탈에 대응되는 도착 위치에서 시작함
        if (playerState.SpawnPointTag != null)
        {
            GameObject spawnPoint = GameObject.FindGameObjectWithTag(playerState.SpawnPointTag);
            Assert.IsNotNull(spawnPoint);

            rb.MovePosition(spawnPoint.transform.position);
        }
        
        InitializeGearSystem(playerState.GearSystemState, playerState.AttackSpeed, playerState.MoveSpeed);
        InitializeHitbox(playerState.AttackDamage);

        playerState.HP.OnValueChange.AddListener(UpdateHealthBarUI);

        // TODO: 인벤토리 상태 초기화하기 (아이템 종류, 쿨타임 등)
        // TODO: playerState.PlayerType에 따른 animator 설정 등 처리하기
    }

    private void InitializeGearSystem(GearSystemState gearSystemState, CharacterStat attackSpeed, CharacterStat moveSpeed)
    {
        // 기어 단계가 바뀔 때마다 공격력 및 공격 속도 버프 수치 갱신
        gearSystem.OnGearLevelChange.AddListener(() => gearSystem.UpdateGearLevelBuff(attackSpeed, moveSpeed));
        gearSystem.InitializeState(gearSystemState);
    }

    // 무기와 긴급 회피 모션의 밀쳐내기 히트박스를 비활성화 상태로 준비함
    private void InitializeHitbox(CharacterStat attackDamage)
    {
        // 공격 성공한 시점을 기어 시스템에게 알려주기 위해 ApplyDamageOnContact 컴포넌트에 콜백 등록
        weaponHitbox.OnApplyDamageSuccess.AddListener(gearSystem.OnAttackSuccess);

        // 해당 컴포넌트에서 플레이어의 공격력 스탯을 사용하도록 설정
        weaponHitbox.RawDamage = attackDamage;
        emergencyEvadeHitbox.RawDamage = attackDamage;

        // 히트박스는 항상 비활성화 상태로 시작해야 함
        weaponHitbox.IsHitboxEnabled = false;
        emergencyEvadeHitbox.IsHitboxEnabled = false;
    }

    private void UpdateHealthBarUI(float diff)
    {
        Debug.Log($"hp 변동: {playerState.HP.CurrentValue}");
        healthBarAnimation.UpdateCurrentValue(playerState.HP.CurrentValue / playerState.HP.MaxValue);
    }

    private void OnEnable()
    {    
        var playerActions = InputManager.InputActions.Player;
        playerActions.Jump.performed += OnJump;
        playerActions.FallDown.performed += OnFallDown;
        playerActions.PerformAction.performed += OnPerformAction;
        playerActions.Evade.performed += OnEvade;
    }

    private void OnDisable()
    {
        var playerActions = InputManager.InputActions.Player;
        playerActions.Jump.performed -= OnJump;
        playerActions.FallDown.performed -= OnFallDown;
        playerActions.PerformAction.performed -= OnPerformAction;
        playerActions.Evade.performed -= OnEvade;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        isJumpKeyPressed = true;
    }

    private void OnFallDown(InputAction.CallbackContext context)
    {
        if (platformerMovement.IsSteppingOnOneWayPlatform)
        {
            platformerMovement.FallThroughPlatform();
        }
    }

    private void OnPerformAction(InputAction.CallbackContext context)
    {
        // 1순위 행동인 상호작용을 먼저 시도 (아이템 줍기, NPC와 대화)
        bool isInteractionSuccessful = interactionManager.TryStartInteractionWithLatestTarget();

        // 만약 상호작용이 가능한 대상이 없었다면 2순위 행동인 공격을 시도한다.
        if (!isInteractionSuccessful)
        {
            // 회피 중이거나 경직/기절 상태이거나 벽에 매달린 경우는 공격 불가.
            // 더이상 다음 공격 모션과 자연스럽게 이어지지 않는 경우도 공격키 입력을 무시한다.
            if (ActionState == PlayerActionState.Evade ||
                ActionState == PlayerActionState.Stagger ||
                ActionState == PlayerActionState.Stun ||
                ActionState == PlayerActionState.StickToWall ||
                ActionState == PlayerActionState.AttackWaitingEnd)
            {
                return;
            }

            // 이미 핵심 공격 모션이 어느 정도 재생된 상태라면 선입력으로 처리,
            // 아니라면 다음 공격 모션 재생
            if (ActionState == PlayerActionState.AttackInProgress)
            {
                HandleAttackInputBuffering();
            }
            else
            {
                TriggerNextAttack();
            }
        }
    }

    private void OnEvade(InputAction.CallbackContext context)
    {
        // 이미 회피 중이거나 기절 상태라면 처리 x
        if (ActionState == PlayerActionState.Evade || ActionState == PlayerActionState.Stun)
        {
            return;
        }

        // Case 1) 상단 방향키 + 회피 버튼 = 기어 게이지 상승
        if (InputManager.InputActions.Player.UpArrow.IsPressed())
        {
            HandleGearGaugeAscent();
        }
        // Case 2) 하단 방향키 + 회피 버튼 = 긴급 회피
        else if (InputManager.InputActions.Player.DownArrow.IsPressed())
        {
            HandleEmergencyEvasion();
        }
        // Case 3) 방향키 입력 없이 회피 버튼 = 일반 회피
        else
        {
            HandleNormalEvasion();
        }
    }

    // 기어 게이지가 현재 단계의 최대치를 일정 시간 유지한 상태에서
    // 유저가 shift + up arrow를 입력한 경우 호출되는 함수.
    //
    // 기어를 한 단계 올리고 특수 공격을 시전한다.
    private void HandleGearGaugeAscent()
    {
        if (gearSystem.IsNextGearLevelReady())
        {
            gearSystem.IncreaseGearLevel();

            // TODO: 특수 공격
        }
    }

    void HandleEmergencyEvasion()
    {
        // 아직 회피 쿨타임이 끝나지 않았다면 처리 x
        if (timeSinceLastEmergencyEvasion < emergencyEvasionCooltime)
        {
            // TODO: 효과음 / 이펙트 등으로 유저에게 지금은 회피를 할 수 없다는 피드백 주기
            Debug.Log("아직 긴급회피 쿨타임이 끝나지 않았습니다...");
            return;
        }

        // TODO: 테스트 끝나면 돌려놓기
        // 테스트 과정에서 긴급 회피를 제한 없이
        // 사용할 수 있도록 기어 게이지 소모를 비활성화해둠
        // if (gearSystem.IsEmergencyEvadePossible())
        {
            // gearSystem.OnEmergencyEvade();

            // 이미 피격당했지만 긴급 회피로 무효화할 수 있는 기간인 경우
            if (numPendingDamages > 0)
            {
                CancelPendingDamages();
            }

            // 공격 도중에 긴급 회피를 사용하는 경우 히트박스를 다시 비활성화해줘야 함
            CancelCurrentAction();
            
            animator.SetTrigger("EmergencyEvade");

            // 회피 무적 상태로 전환
            ActionState = PlayerActionState.Evade;
            isInvincible = true;

            // 회피 도중에는 추락 및 넉백 x
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        
            // 쿨타임 계산 시작
            timeSinceLastEmergencyEvasion = 0f;
            
            // 챕터1 박스 기믹에서 일반 회피에만 반응할 수 있게 회피 타입 기록
            IsNormalEvasion = false;
        }
    }

    // 긴급 회피가 시전된 경우 아직 적용되지 않은 데미지들을 무효화함.
    // 조금 더 자세한 내용은 ApplyDamageWithDelay() 참고할 것.
    private void CancelPendingDamages()
    {
        damageCancellation.Cancel();
        damageCancellation.Dispose();
        damageCancellation = new CancellationTokenSource();
    }

    private void HandleNormalEvasion()
    {
        // 아직 회피 쿨타임이 끝나지 않았다면 처리 x
        if (timeSinceLastEvasion < evasionCooltime)
        {
            // TODO: 효과음 / 이펙트 등으로 유저에게 지금은 회피를 할 수 없다는 피드백 주기
            Debug.Log("아직 회피 쿨타임이 끝나지 않았습니다...");
            return;
        }

        // 공격 애니메이션 중 타격 부분이 재생 중인 경우에도 처리 x
        if (weaponHitbox.IsHitboxEnabled)
        {
            return;
        }

        CancelCurrentAction();

        // 입력 방향으로 방향 전환
        UpdateFacingDirectionByInput();

        // 회피 무적 상태로 전환
        ActionState = PlayerActionState.Evade;
        isInvincible = true;

        // 회피 도중에는 추락 및 넉백 x
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        // 지면에 멈춰있는 상태에서는 엄청 큰 마찰력이 사용되므로
        // 확실히 마찰력을 없애주지 않으면 땅 위에 가만히 멈춰있을 위험이 있음.
        platformerMovement.ApplyZeroFriction();
        
        // 쿨타임 계산 시작
        timeSinceLastEvasion = 0f;

        // 회피 모션 재생
        animator.SetTrigger("Evade");

        // 챕터1 박스 기믹에서 일반 회피에만 반응할 수 있게 회피 타입 기록
        IsNormalEvasion = true;

        float targetX = transform.position.x + evasionDistance * (IsFacingLeft ? -1f : 1f);
        rb.DOMoveX(targetX, evasionDuration)
            .SetEase(evasionEase)
            .SetUpdate(UpdateType.Fixed);

        screenShake.ApplyScreenShake(strength: 0.05f, duration: 0.2f, frequencyGain: 2f);
    }

    // 회피 애니메이션의 마지막 프레임에 호출되는 이벤트.
    // 무적 판정을 해제하고 기본 이동 상태로 돌아온다.
    public void OnEvadeEnd()
    {
        ActionState = PlayerActionState.IdleOrRun;
        isInvincible = false;

        // 회피 끝났으면 다시 추락 가능
        platformerMovement.UseDefaultGravityScale();
    }

    // 공격 애니메이션 중 타격 프레임이 지나간 이후부터
    // 다음 공격으로 이어나갈 수 있는 시점 사이에
    // 공격 키를 누르면 선입력으로 처리됨.
    //
    // 타격 프레임 이전에는 선입력으로 취급하지 않는 이유:
    // 생각보다 공격 애니메이션이 길어서 내가 키를 누른 시점과
    // 선입력에 의한 자동적인 다음 공격 트리거의 시간적 차이가 무척 커질 수 있음.
    // "난 키 입력을 멈췄는데도 공격이 나가네?"라는 느낌을 없애기 위한 조치라고 보면 됨.
    private void HandleAttackInputBuffering()
    {
        if (isAttackInputBufferingAllowed)
        {
            isAttackInputBuffered = true;
        }
    }

    private void TriggerNextAttack()
    {
        // 이미 공중 공격을 했으면 착지하기 전까지는 공격 불가
        if (!platformerMovement.IsGrounded && isAirAttackPerformed)
        {
            return;
        }

        // 공격을 시작하는 순간에 한해 방향 전환 허용
        UpdateFacingDirectionByInput();

        // 가로로 움직이는 플랫폼 위에서도 가만히 서있도록 큰 마찰력 사용
        platformerMovement.ApplyHighFriction();

        // 다음 공격 모션 선택
        if (attackCount < maxAttackCount)
        {
            attackCount++;
        }
        else
        {
            attackCount = 1;
        }

        // 공중에서는 최대 1회까지만 공격 가능
        if (!platformerMovement.IsGrounded)
        {
            isAirAttackPerformed = true;
        }

        // 연속 공격의 트리거 이름은 Attack1, Attack2, ..., AttackN 형태로 주어짐
        animator.SetTrigger($"Attack{attackCount}");

        spriteRootMotion.HandleAnimationChange();

        // 너무 시간차가 큰 선입력 방지하기 위해 모션의 앞부분에는 선입력 처리 x
        isAttackInputBufferingAllowed = false;

        ActionState = PlayerActionState.AttackInProgress;
    }

    public void OnEnableAttackCollider()
    {
        // 공격 판정이 들어가는 FixedUpdate보다 애니메이션 상태 갱신이
        // 나중에 일어나기 때문에 이 함수가 호출되는 프레임에 정확히 피격 경직을 당하는 경우
        // Stagger 상태에서 무기 히트박스를 켜버리는 상황이 발생할 수 있음!
        //
        // Note:
        // 어떤 콜라이더를 활성화할지는 지금이 몇 번째 타격인지에 따라 애니메이션 클립에서 처리한다
        // 플레이어의 공격 모션마다 타격 범위가 다르기 때문
        // ex) loyal 타입 4타는 지팡이에서 총을 쏘는 모션이라 훨씬 앞에 히트박스가 있어야 함
        if (ActionState == PlayerActionState.Stagger)
        {
            OnDisableAttackCollider();
            return;
        }

        // 바라보는 방향에 따라 콜라이더 위치 조정
        weaponHitbox.SetHitboxDirection(IsFacingLeft);
    }

    public void OnBeginAttackInputBuffering()
    {
        isAttackInputBufferingAllowed = true;
    }

    public void OnDisableAttackCollider()
    {
        weaponHitbox.IsHitboxEnabled = false;
    }

    // 공격 애니메이션이 완전히 종료되는 시점에 호출되는 이벤트.
    // 공격 상태를 종료하고 IdleOrRun 상태로 복귀함.
    public void OnAttackMotionEnd()
    {
        // 마지막 모션의 경우 별도의 OnStartWaitingAttackContinuation() 이벤트 없이
        // 바로 OnAttackMotionEnd()가 호출되므로 선입력이 있는 경우를 따로 체크해야 함.
        // 공중에 있는 경우는 최대 공격 횟수에 도달하면 무조건 공격을 멈춰야 하므로 취급 x
        if (attackCount == maxAttackCount && platformerMovement.IsGrounded && isAttackInputBuffered)
        {
            isAttackInputBuffered = false;
            TriggerNextAttack();
        }
        else
        {
            CancelCurrentAction();
        }
    }

    // 공격 애니메이션에서 선입력이 있다면 다음 공격으로 넘어가야 할 시점에 호출되는 이벤트.
    // 공격 키를 정확히 그 시점에 누른 것과 동일한 효과를 준다.
    // 선입력이 없었다면 애니메이션이 완전히 끝나기 전까지 공격 입력을 기다리는 상태에 진입.
    public void OnStartWaitingAttackContinuation()
    {
        if (isAttackInputBuffered)
        {
            isAttackInputBuffered = false;
            TriggerNextAttack();
        }
        else
        {
            ActionState = PlayerActionState.AttackWaitingContinuation;
        }
    }

    // loyal 타입 3타처럼 다음 공격으로 자동으로 넘어가는 프레임을 지나치면
    // 추가 입력 대기 없이 모션 끝까지 기다리는게 자연스러운 경우 사용되는 애니메이션 이벤트.
    // 모션이 끝날 때까지 공격키에 반응하지 않도록 만든다.
    public void OnStopWaitingAttackContinuation()
    {
        isAttackInputBuffered = false;
        ActionState = PlayerActionState.AttackWaitingEnd;
    }

    private void FixedUpdate()
    {
        if (ActionState == PlayerActionState.Evade)
        {
            // 회피 도중에는 아무것도 처리하지 않음
        }
        // one way platform을 위로 스쳐 지나가는 상황에서
        // 공격 상태에 진입해 정지하면 IsGrounded가 true가 되어버림.
        // 실제로는 공중에 떠 있는 것으로 취급해야 하므로 공격 중이 아닐 때만 상태를 갱신함.
        else
        {
            // 공격 중이라면 애니메이션의 pivot 변화에 따라 움직임을 부여.
            // animator에 Apply Root Motion을 체크하는 것으로는 이러한 움직임이 재현되지 않아
            // 부득이하게 비슷한 기능을 직접 만들어 사용하게 되었음...
            if (IsAttacking())
            {
                spriteRootMotion.ApplyVelocity(IsFacingLeft);
            }

            platformerMovement.HandleGroundContact();
            if (platformerMovement.IsGrounded)
            {
                // 공격 모션 중에서 예외적으로 착지 모션이 공중 공격 모션보다 우선순위가 높아서
                // 딱 착지하기 직전에 공중 공격을 하면 OnAttackMotionEnd() 등이
                // 호출되지 않은 상태에서 착지 모션으로 전환될 수 있음.
                // 이 경우 수동으로 공격 상태를 정리해줘야 함...
                if (isAirAttackPerformed && IsAttacking())
                {
                    CancelCurrentAction();
                }
                isAirAttackPerformed = false;

                // 벽에 붙은 상태에서 엘리베이터가 올라와
                // IsGrounded = true가 되어버리는 상황 처리
                // TODO: PlatformerMovement 스크립트로 이식
                if (ActionState == PlayerActionState.StickToWall)
                {
                    platformerMovement.StopStickingToWall();
                }
            }
            // TODO: PlatformerMovement 스크립트 테스트 끝나면 삭제할 것
            // else
            // {
            //     if (ActionState == PlayerActionState.IdleOrRun || IsAttacking())
            //     {
            //         HandleCoyoteTime();
            //         HandleFallingVelocity();
            //     }
            // }
        }

        HandleEvasionCooltime();

        HandleMoveInput();

        if (isJumpKeyPressed)
        {
            HandleJumpInput();
        }

        if (ActionState == PlayerActionState.Stagger)
        {
            // 넉백 효과로 생긴 velocity 부드럽게 감소
            platformerMovement.UpdateMoveVelocity(0f);
        }

        // AdjustCollider();
        UpdateCameraFollowTarget();
        UpdateAnimatorState();
    }

    private void HandleEvasionCooltime()
    {
        timeSinceLastEvasion += Time.fixedDeltaTime;
        timeSinceLastEmergencyEvasion += Time.fixedDeltaTime;
    }

    private void HandleMoveInput()
    {
        float moveInput = InputManager.InputActions.Player.Move.ReadValue<float>();
        if (ActionState == PlayerActionState.IdleOrRun)
        {
            UpdateFacingDirectionByInput();
            if (playerState.PlayerType == PlayerType.Rogue && platformerMovement.ShouldStickToWall(moveInput))
            {
                ActionState = PlayerActionState.StickToWall;
                platformerMovement.StartStickingToWall(moveInput);
            }
            else
            {
                float desiredSpeed = playerState.MoveSpeed.CurrentValue * moveInput;
                platformerMovement.UpdateMoveVelocity(desiredSpeed);
                platformerMovement.UpdateFriction(desiredSpeed);
            }
        }
        else if (ActionState == PlayerActionState.StickToWall && platformerMovement.ShouldStopStickingToWall(moveInput))
        {
            ActionState = PlayerActionState.IdleOrRun;
            platformerMovement.StopStickingToWall();
        }
    }

    // 이동 입력에 따라 바라보는 방향을 변경함
    // 키가 입력되지 않은 상황에서는 기존 방향을 유지
    private void UpdateFacingDirectionByInput()
    {
        var moveInput = InputManager.InputActions.Player.Move.ReadValue<float>();
        if (moveInput != 0f)
        {
            IsFacingLeft = moveInput < 0f;
        }
    }

    private void HandleJumpInput()
    {
        // 공격, 경직 등 다른 상태에서는 점프 불가능
        if (ActionState == PlayerActionState.IdleOrRun || ActionState == PlayerActionState.StickToWall)
        {
            bool success = platformerMovement.TryJump();
            if (success)
            {
                // 점프 애니메이션 재생
                animator.SetTrigger("Jump");

                // 점프하는 방향 바라보기
                IsFacingLeft = rb.velocity.x < 0f;
            }
        }

        // 입력 처리 완료
        isJumpKeyPressed = false;
    }


    // 평지에서 점프할 때 카메라가 위 아래로 흔들리는 것을 방지하기 위해
    // 카메라 추적 대상을 플레이어와 별개의 오브젝트로 설정하고
    // 플랫폼에 착지했을 때만 플레이어의 y 좌표를 따라가도록 설정함.
    // x 좌표의 경우 플레이어의 실시간 위치 + 바라보는 방향으로 look ahead.
    //
    // Note:
    // 맵이 수직으로 그리 높지 않은 경우는 괜찮은데
    // 절벽처럼 카메라 시야를 벗어날 정도로 낙하하는 경우에는
    // 캐릭터가 갑자기 화면 밖으로 사라지니까 이상하다고 느낄 수 있음.
    private void UpdateCameraFollowTarget()
    {
        Vector2 newPosition = transform.position;

        // 바라보는 방향으로 look ahead
        newPosition.x += IsFacingLeft ? -cameraLookAheadDistance : cameraLookAheadDistance;

        // 벽에 매달리거나 새로운 플랫폼에 착지하지 않았다면 y 좌표는 유지.
        if (!platformerMovement.IsGrounded && ActionState != PlayerActionState.StickToWall)
        {
            newPosition.y = cameraFollowTarget.transform.position.y;
        }

        cameraFollowTarget.transform.position = newPosition;
    }

    // 매 프레임 갱신해야 하는 애니메이터 파라미터 관리
    private void UpdateAnimatorState()
    {
        animator.SetBool("IsGrounded", platformerMovement.IsGrounded);
        animator.SetFloat("HorizontalVelocity", rb.velocity.y);
        animator.SetBool("IsRunning", InputManager.InputActions.Player.Move.IsPressed());
        animator.SetBool("IsAttacking", IsAttacking());
        animator.SetBool("IsStaggered", ActionState == PlayerActionState.Stagger);
        animator.SetBool("IsStunned", ActionState == PlayerActionState.Stun);
        animator.SetBool("IsEvading", ActionState == PlayerActionState.Evade);
        animator.SetBool("IsStickingToWall", ActionState == PlayerActionState.StickToWall);
        animator.SetFloat("AttackSpeed", playerState.AttackSpeed.CurrentValue);
    }

    private bool IsAttacking()
    {
        return ActionState == PlayerActionState.AttackInProgress ||
            ActionState == PlayerActionState.AttackWaitingContinuation ||
            ActionState == PlayerActionState.AttackWaitingEnd;
    }

    CharacterStat IDestructible.GetHPStat()
    {
        return playerState.HP;
    }

    CharacterStat IDestructible.GetDefenseStat()
    {
        return playerState.Defense;
    }

    Team IDestructible.GetTeam()
    {
        return Team.Player;
    }

    bool IDestructible.IsInvincible()
    {
        return isInvincible;
    }

    bool IDestructible.OnDamage(Team damageSource, float finalDamage, StaggerInfo staggerInfo)
    {
        flashEffectOnHit.StartEffectAsync().Forget();

        switch(staggerInfo.strength)
        {
            case StaggerStrength.Weak:
                ApplyStagger(staggerInfo.direction * weakStaggerKnockbackForce, staggerInfo.duration);
                break;
            case StaggerStrength.Strong:
                ApplyStagger(staggerInfo.direction * strongStaggerKnockbackForce, staggerInfo.duration);
                break;
        }

        ApplyDamageWithDelayAsync(finalDamage, emergencyEvasionTimeWindow, cancellationToken: damageCancellation.Token).Forget();

        return true;
    }

    // 플레이어가 긴급회피로 데미지를 무효화할 수 있으니 잠시 유예 시간을 부여함.
    //
    // 회피 버튼을 눌렀을 때 긴급 회피로 처리해야 하는지 확인할 수 있도록
    // 대기 중인 데미지의 수를 numPendingDamages 변수로 관리한다.
    private async UniTask ApplyDamageWithDelayAsync(float finalDamage, float delay, CancellationToken cancellationToken)
    {
        numPendingDamages++;
        await UniTask.WaitForSeconds(delay, cancellationToken: cancellationToken).SuppressCancellationThrow();
        numPendingDamages--;

        // 긴급 회피가 시전되지 않은 경우에만 실제 데미지로 처리
        if (!cancellationToken.IsCancellationRequested)
        {
            (this as IDestructible).HandleHPDecrease(finalDamage);

            // 공격 당하면 게이지가 깎임
            gearSystem.OnPlayerHit();
        }
    }

    // 현재 하던 행동을 취소하고 피격 경직 상태에 진입
    private void ApplyStagger(Vector2 staggerForce, float staggerDuration)
    {
        CancelCurrentAction();

        rb.velocity = staggerForce;

        // 가만히 서있는 상태인 경우 마찰력이 높은 상태를 유지하므로
        // 움직이다가 피격당하는 경우와 밀려나는 정도가 일관적이지 않음!
        // 그러니 일단 넉백 당한다 하면 무조건 마찰력를 없애줘야 함.
        platformerMovement.ApplyZeroFriction();
        SetStaggerStateForDurationAsync(staggerDuration).Forget();

        // TO:
        // 경직 애니메이션 재생 (약한 경직 -> 제자리 경직 모션, 강한 경직 -> 뒤로 넘어지는 모션)
        // 지금은 점프 모션 중 프레임 하나 훔쳐와서 경직 모션이라 치고 박아둔 상태 (player_loyal_stagger_temp)이고,
        // 애니메이터의 IsStaggered 파라미터를 설정해서 임시 경직 애니메이션을 재생하도록 했음.
        //
        // 경직 모션 두 개 완성되면 UpdateAnimatorState() 함수랑 애니메이션 상태 그래프 수정해야 함
    }

    // 경직에 걸리거나 기절당하는 등 현재 하던 행동을 종료해야 하는 경우 사용.
    // 모든 상태를 깔끔하게 정리하고 IdleOrRun 상태로 복귀함.
    private void CancelCurrentAction()
    {
        if (ActionState == PlayerActionState.StickToWall)
        {
            platformerMovement.StopStickingToWall();
        }
        else if (ActionState == PlayerActionState.Stagger)
        {
            // CancellationTokenSource는 리셋이 불가능해서
            // 한 번 cancel하면 새로 만들어줘야 함.
            staggerCancellation.Cancel();
            staggerCancellation.Dispose();
            staggerCancellation = new();
        }
        else if (ActionState == PlayerActionState.Stun)
        {
            stunCancellation.Cancel();
            stunCancellation.Dispose();
            stunCancellation = new();
        }
        else if (ActionState == PlayerActionState.Evade)
        {
            // 회피 이동을 처리하던 tweening 취소
            rb.DOKill();

            // 회피 상태 취소
            OnEvadeEnd();
        }
        else if (IsAttacking())
        {
            attackCount = 0;
            isAttackInputBufferingAllowed = false;
            isAttackInputBuffered = false;
            OnDisableAttackCollider();
        }

        ActionState = PlayerActionState.IdleOrRun;
    }

    private async UniTask SetStaggerStateForDurationAsync(float duration)
    {
        ActionState = PlayerActionState.Stagger;

        await UniTask.WaitForSeconds(duration, cancellationToken: staggerCancellation.Token);

        ActionState = PlayerActionState.IdleOrRun;
    }

    void IDestructible.OnDestruction()
    {
        // TODO: 잠시 입력 비활성화, 은신처로 복귀 (결과창 표시?)
        animator.SetTrigger("Death");
    }

    // 챕터1 박스 기믹 등 특수한 상황에만 부여되는 기절 효과.
    // 기절 상태에서 공격을 당하면 기절이 풀리고 경직 상태로 전환된다.
    public async UniTask ApplyStunForDurationAsync(float duration)
    {
        CancelCurrentAction();

        ActionState = PlayerActionState.Stun;
        animator.SetTrigger("Stun");

        await UniTask.WaitForSeconds(duration, cancellationToken: stunCancellation.Token);

        ActionState = PlayerActionState.IdleOrRun;
    }

    // 챕터1 보스의 돌진 패턴에 맞아 끌려간 뒤 벽에 튕겨나오는 모션.
    // 그냥 힘만 주면 플레이어 조작에 의해 바로 정지해버리니까 잠시 경직 상태를 부여한다.
    // distance는 부호를 고려한 튕겨나올 거리임 (왼쪽으로 튕겨나오면 음수)
    public async UniTask ApplyWallReboundAsync(float distance, float duration)
    {
        ActionState = PlayerActionState.Stun;
        platformerMovement.ApplyZeroFriction();
        IsFacingLeft = distance > 0f;

        rb.DOMoveX(rb.position.x + distance, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(UpdateType.Fixed);

        await UniTask.WaitForSeconds(duration);

        ActionState = PlayerActionState.IdleOrRun;
    }






    // 본인 이 함수 어따가 달아야할지 모르겠음
    // 좋은 아이디어 추천 바람
    private void PlayAttack1and2SFX() {
        int[] attack1and2Indices = {0, 1, 2, 3, 4};
        AudioManager.instance.PlaySFX(attack1and2Indices);
    }
    private void PlayAttack3SFX() {
        int[] attack3Indices = {5, 6};
        AudioManager.instance.PlaySFX(attack3Indices);
    }
    private void PlayAttack4SFX() {
        int[] attack4Indices = {7, 8};
        AudioManager.instance.PlaySFX(attack4Indices);
    }
    private void PlayLandSFX() {
        int[] landingIndices = {9};
        AudioManager.instance.PlaySFX(landingIndices);        
    }
    private void PlayDashSFX() {
        int[] dashingIndices = {10};
        AudioManager.instance.PlaySFX(dashingIndices);        
    }
    private void PlayEscapeSFX() {
        int[] escapingIndices = {11};
        AudioManager.instance.PlaySFX(escapingIndices);        
    }
    private void PlayJumpingSFX() {
        int[] jumpingIndices = {18};
        AudioManager.instance.PlaySFX(jumpingIndices);        
    }
}
