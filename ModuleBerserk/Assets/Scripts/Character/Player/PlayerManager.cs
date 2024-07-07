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
    [Header("Walk / Run")]
    [SerializeField] private float turnAcceleration = 150f;
    [SerializeField] private float moveAcceleration = 100f;
    [SerializeField] private float moveDecceleration = 150f;
    // 서있을 때는 마찰력을 높게 줘서 경사로에서도 미끄러지지 않도록 만듦
    [SerializeField] private PhysicsMaterial2D zeroFrictionMat;
    [SerializeField] private PhysicsMaterial2D maxFrictionMat;


    [Header("Jump / Fall")]
    [SerializeField] private float jumpVelocity = 12f;
    [SerializeField] private Vector2 wallJumpVelocity = new(10f, 10f);
    // 땅에서 떨어져도 점프를 허용하는 time window
    [SerializeField] private float coyoteTime = 0.15f;
    // 공중에 있지만 위로 이동하는 중이라면 DefaultGravityScale을 사용하고
    // 아래로 이동하는 중이라면 GravityScaleWhenFalling을 사용해
    // 더 빨리 추락해서 공중에 붕 뜨는 이상한 느낌을 줄임.
    [SerializeField] private float defaultGravityScale = 3f;
    [SerializeField] private float gravityScaleWhileFalling = 6f;
    // 아주 높은 곳에서 떨어질 때 부담스러울 정도로 아래로 가속하는 상황 방지
    [SerializeField] private float maxFallSpeed = 15f;
    // 공중 조작이 지상 조작보다 둔하게 만드는 파라미터 (0: 조작 불가, 1: 변화 없음)
    [SerializeField, Range(0f, 1f)] private float defaultAirControl = 0.5f;
    // wall jump 이후 벽으로 돌아오는데에 걸리는 시간을 조정하는 파라미터 (airControl을 잠시 이 값으로 대체함)
    [SerializeField, Range(0f, 1f)] private float airControlWhileWallJumping = 0.2f;
    // wall jump 이후 defaultAirControl 대신 airControlWhileWallJumping을 적용할 기간
    [SerializeField, Range(0f, 1f)] private float wallJumpAirControlPenaltyDuration = 0.3f;


    [Header("Ground Contact")]
    // 땅으로 취급할 layer를 모두 에디터에서 지정해줘야 함!
    [SerializeField] private LayerMask groundLayerMask;
    // 콜라이더로부터 이 거리보다 가까우면 접촉 중이라고 취급.
    // 
    // 바닥과의 충돌 판정 거리는 생각보다 크게 줘야 함.
    // 엘리베이터를 타고 하강하는 상황이라면
    // 엘리베이터가 먼저 떨어지고 플레이어가 중력으로 낙하하는
    // 구조여서 의외로 수직 거리가 크게 벌어지기 때문!
    //
    // 벽과의 충돌은 충돌 판정 거리를 작게 잡아줘도 됨.
    [SerializeField] private float groundContactDistanceThreshold = 0.2f;
    [SerializeField] private float wallContactDistanceThreshold = 0.02f;


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
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private SpriteRootMotion spriteRootMotion;
    private InteractionManager interactionManager;
    private FlashEffectOnHit flashEffectOnHit;
    private GearSystem gearSystem;

    // GameState에서 가져온 저장 가능한 플레이어 상태들
    private PlayerState playerState;

    // 지면 접촉 테스트 관리자
    private GroundContact groundContact;
    // 땅에서 떨어진 시점부터 Time.deltaTime을 누적하는 카운터로,
    // 이 값이 CoyoteTime보다 낮을 경우 isGrounded가 false여도 점프 가능.
    private float coyoteTimeCounter = 0f;
    // 땅에 닿기 전에 점프한 횟수.
    // 더블 점프 처리에 사용됨.
    private int jumpCount = 0;
    // 벽에 붙어있다가 떨어지는 순간의 coyote time과
    // 그냥 달리다가 떨어지는 순간의 coyote time을 구분하기 위한 상태 변수.
    // 점프를 일반 점프로 할지 wall jump로 할지 결정한다.
    private bool shouldWallJump = false;
    // 키 입력은 physics 루프와 다른 시점에 처리되니까
    // 여기에 기록해두고 물리 연산은 FixedUpdate에서 처리함
    private bool isJumpKeyPressed = false;
    // 현재 어느 쪽을 바라보고 있는지 기록.
    // 스프라이트 반전과 카메라 추적 위치 조정에 사용됨.
    private bool isStickingToRightWall;
    // defaultAirControl과 airControlWhileWallJumping 중 실제로 적용될 수치
    private float airControl;
    
    //Prototype 공격용 변수들
    private bool isAttackInputBufferingAllowed = false; // 공격 모션 중에서 선입력 기록이 가능한 시점에 도달했는지
    private bool isAttackInputBuffered = false; // 공격 버튼 선입력 여부
    private bool isAirAttackPerformed = false; // 공중 공격을 이미 했는지 (점프마다 한 번 가능)
    private int attackCount = 0;
    private int maxAttackCount = 2; // 최대 연속 공격 횟수. attackCount가 이보다 커지면 첫 공격 모션으로 돌아감.

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
        
        groundContact = new(rb, boxCollider, groundLayerMask, groundContactDistanceThreshold, wallContactDistanceThreshold);
        airControl = defaultAirControl;
    }

    private void FindComponentReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        spriteRootMotion = GetComponent<SpriteRootMotion>();
        interactionManager = GetComponent<InteractionManager>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
        gearSystem = GetComponent<GearSystem>();
    }

    private void Start()
    {
        InitializePlayerState();
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

    private void OnEnable()
    {    
        var playerActions = InputManager.InputActions.Player;
        playerActions.Jump.performed += OnJump;
        playerActions.FallDown.performed += OnFallDown;
        playerActions.PerformAction.performed += OnPerformAction;
        playerActions.Evade.performed += OnEvade;

        // TODO: playerState.HP.OnValueChange에 체력바 UI 업데이트 함수 등록
    }

    private void OnDisable()
    {
        var playerActions = InputManager.InputActions.Player;
        playerActions.Jump.performed -= OnJump;
        playerActions.FallDown.performed -= OnFallDown;
        playerActions.PerformAction.performed -= OnPerformAction;
        playerActions.Evade.performed -= OnEvade;

        // TODO: playerState.HP.OnValueChange에 체력바 UI 업데이트 함수 제거
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        isJumpKeyPressed = true;
    }

    private void OnFallDown(InputAction.CallbackContext context)
    {
        if (groundContact.IsSteppingOnOneWayPlatform())
        {
            StopStickingToElevator();
            groundContact.PreventTestForDuration(0.2f);
            groundContact.IgnoreCurrentPlatformForDurationAsync(0.5f).Forget();
        }
    }

    private void OnPerformAction(InputAction.CallbackContext context)
    {
        // 1순위 행동인 상호작용을 먼저 시도 (아이템 줍기, NPC와 대화)
        bool isInteractionSuccessful = interactionManager.TryStartInteractionWithLatestTarget();

        // 만약 상호작용이 가능한 대상이 없었다면 2순위 행동인 공격을 시도한다.
        if (!isInteractionSuccessful)
        {
            // 회피 중이거나 경직/기절 상태이거나 벽에 매달린 경우는 공격 불가
            // TODO: 회피 모션 중에도 공격 선입력 허용하는 것 고려하기
            if (ActionState == PlayerActionState.Evade || ActionState == PlayerActionState.Stagger || ActionState == PlayerActionState.Stun || ActionState == PlayerActionState.StickToWall)
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
    }

    // 회피 애니메이션의 마지막 프레임에 호출되는 이벤트.
    // 무적 판정을 해제하고 기본 이동 상태로 돌아온다.
    public void OnEvadeEnd()
    {
        ActionState = PlayerActionState.IdleOrRun;
        isInvincible = false;

        // 회피 끝났으면 다시 추락 가능
        rb.gravityScale = defaultGravityScale;
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
        if (!groundContact.IsGrounded && isAirAttackPerformed)
        {
            return;
        }

        // 공격을 시작하는 순간에 한해 방향 전환 허용
        UpdateFacingDirectionByInput();

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
        if (!groundContact.IsGrounded)
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
        if (ActionState == PlayerActionState.Stagger)
        {
            return;
        }

        // 바라보는 방향에 따라 콜라이더 위치 조정
        weaponHitbox.SetHitboxDirection(IsFacingLeft);

        weaponHitbox.IsHitboxEnabled = true;
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
        if (attackCount == maxAttackCount && groundContact.IsGrounded && isAttackInputBuffered)
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

            groundContact.TestContact();
            if (groundContact.IsGrounded)
            {
                // 공격 모션 중에서 예외적으로 착지 모션이 공중 공격 모션보다 우선순위가 높아서
                // 딱 착지하기 직전에 공중 공격을 하면 OnAttackMotionEnd() 등이
                // 호출되지 않은 상태에서 착지 모션으로 전환될 수 있음.
                // 이 경우 수동으로 공격 상태를 정리해줘야 함...
                //
                // Note:
                // ResetJumpRelatedStates()에서 isAirAttackPerformed의 값을
                // false로 바꿔버리므로 반드시 해당 함수보다 먼저 처리해줘야 함!!!
                if (isAirAttackPerformed && IsAttacking())
                {
                    CancelCurrentAction();
                }

                ResetJumpRelatedStates();

                // 움직이는 엘리베이터 위에서도 안정적으로
                // 서있을 수 있게 parent object로 설정해줌
                HandleStickingToElevator();

                // 벽에 붙은 상태에서 엘리베이터가 올라와
                // IsGrounded = true가 되어버리는 상황 처리
                if (ActionState == PlayerActionState.StickToWall)
                {
                    StopStickingToWall();
                }
            }
            else
            {
                StopStickingToElevator();
                if (ActionState == PlayerActionState.IdleOrRun || IsAttacking())
                {
                    HandleCoyoteTime();
                    HandleFallingVelocity();
                }
            }
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
            UpdateMoveVelocity(0f);
        }

        // AdjustCollider();
        UpdateCameraFollowTarget();
        UpdateAnimatorState();
    }

    // 엘리베이터 위에서 벗어났을 때 parent object 설정을 원래대로 돌려놓음.
    // parent 설정이 필요한 이유는 HandleStickingToElevator()의 주석 참고.
    private void StopStickingToElevator()
    {
        transform.SetParent(null);
    }

    // IsGrounded가 true인 경우 호출되는 함수로,
    // 밟고 있는 플랫폼이 엘리베이터인 경우 플랫폼을 자신의 parent object로 설정한다.
    //
    // 아래로 움직이는 엘리베이터의 이동 속도를 중력이
    // 바로 따라잡지 못해 낙하와 착지를 반복하는 현상을 막아줌.
    private void HandleStickingToElevator()
    {
        // GetComponent와 SetParent가 무거운 연산이므로
        // parent가 null인 경우에만 (i.e. 엘리베이터에 서있지 않은 상태) 실행한다.
        if (transform.parent == null && groundContact.CurrentPlatform.GetComponent<Elevator>())
        {
            transform.SetParent(groundContact.CurrentPlatform.transform);
        }
    }

    private void HandleEvasionCooltime()
    {
        timeSinceLastEvasion += Time.fixedDeltaTime;
        timeSinceLastEmergencyEvasion += Time.fixedDeltaTime;
    }

    private void ResetJumpRelatedStates()
    {
        jumpCount = 0;
        isAirAttackPerformed = false;
        shouldWallJump = false;
        coyoteTimeCounter = 0f;
        rb.gravityScale = defaultGravityScale;
    }

    private void HandleCoyoteTime()
    {
        coyoteTimeCounter += Time.fixedDeltaTime;

        // 방금 전까지 벽에 매달려있었더라도 coyote time을 넘어서면
        // 일반적인 더블 점프로 취급 (점프해도 위로만 상승)
        if (coyoteTimeCounter > coyoteTime)
        {
            shouldWallJump = false;
        }
    }
    
    private void HandleFallingVelocity()
    {
        // 현재 추락하는 중이라면 더 강한 중력을 사용해서 붕 뜨는 느낌을 줄임.
        bool isFalling = rb.velocity.y < -0.01f;
        if (isFalling)
        {
            rb.gravityScale = gravityScaleWhileFalling;
        }

        // 최대 추락 속도 제한
        if (rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }
    }

    private void HandleMoveInput()
    {
        float moveInput = InputManager.InputActions.Player.Move.ReadValue<float>();
        if (ActionState == PlayerActionState.IdleOrRun)
        {
            UpdateFacingDirectionByInput();
            if (ShouldStickToWall(moveInput))
            {
                StartStickingToWall(moveInput);
            }
            else
            {
                UpdateMoveVelocity(moveInput);
            }

            ApplyStaticFrictionOnNoInput();
        }
        else if (ActionState == PlayerActionState.StickToWall && ShouldStopStickingToWall(moveInput))
        {
            StopStickingToWall();
        }
    }

    private void ApplyStaticFrictionOnNoInput()
    {
        if (InputManager.InputActions.Player.Move.IsPressed())
        {
            rb.sharedMaterial = zeroFrictionMat;
        }
        else
        {
            rb.sharedMaterial = maxFrictionMat;
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

    // 공중에 있고 이동하려는 방향의 벽과 접촉한 경우에 한해 true 반환.
    private bool ShouldStickToWall(float moveInput)
    {
        // TODO:
        // 1. 벽붙기는 로그 타입만 가능하도록 수정
        // 2. 이미 한 번 붙었다가 떨어진 벽에는 다시 붙을 수 없도록 제한 (무한 벽타기 방지)
        bool shouldStickRight = moveInput > 0f && groundContact.IsInContactWithRightWall;
        bool shouldStickLeft = moveInput < 0f && groundContact.IsInContactWithLeftWall;
        return !groundContact.IsGrounded && (shouldStickRight || shouldStickLeft);
    }

    // 벽에 붙은 방향과 반대로 이동하는 경우 벽붙기 중지
    private bool ShouldStopStickingToWall(float moveInput)
    {
        return
            (isStickingToRightWall && moveInput < 0f) || // 오른쪽 벽에 붙은 상태에서 왼쪽으로 이동
            (!isStickingToRightWall && moveInput > 0f); // 왼쪽 벽에 붙은 상태에서 오른쪽으로 이동
    }

    private void StartStickingToWall(float moveInput)
    {
        ActionState = PlayerActionState.StickToWall;

        // 매달린 방향과 반대로 이동하는 경우 매달리기 취소해야 하므로 현재 방향 기록
        isStickingToRightWall = moveInput > 0f;

        // wall jump 가능하게 설정
        jumpCount = 0;
        shouldWallJump = true;

        // coyote time 리셋
        coyoteTimeCounter = 0f;

        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;

        // TODO: 벽에 붙어도 공중 공격 가능 여부를 초기화해야 한다면 isAirAttackPossible = true 넣기
    }

    private void StopStickingToWall()
    {
        ActionState = PlayerActionState.IdleOrRun;

        // 중력 활성화
        rb.gravityScale = defaultGravityScale;
    }

    // 지면에 서있는 경우는 지면과 평행하게, 공중인 경우는 그냥 좌우로 이동.
    //
    // Note:
    // 경사로에 있는 경우 GroundTangent가 수평이 아니다!
    // 이 상황에서 수평으로 움직이면 낙하-착지를 반복하게 될 수 있으므로
    // 지면과 평행하게 움직여주는게 중요함.
    private void UpdateMoveVelocity(float moveInput)
    {
        // 도달하고 싶은 속도 (좌우 방향 고려 o)
        float desiredSpeed = playerState.MoveSpeed.CurrentValue * moveInput;

        // 가속해야 할 방향으로의 속도 성분 (공중인 경우 중력 고려 x)
        Vector2 moveDirection = groundContact.IsGrounded ? groundContact.GroundTangent : Vector2.right;
        float currentSpeed = Vector2.Dot(moveDirection, rb.velocity);

        // 방향 전환 여부에 따라 다른 가속도 사용
        float acceleration = ChooseAcceleration(moveInput, desiredSpeed);

        // 공중이라면 AirControl 수치(0.0 ~ 1.0)에 비례해 가속도 감소
        if (!groundContact.IsGrounded)
        {
            acceleration *= airControl;
        }

        // 원하는 속도에 부드럽게 도달하도록 보간 (공중인 경우 수직 속도 변경 x)
        float updatedSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * Time.deltaTime);
        Vector2 updatedVelocity = moveDirection * updatedSpeed;
        if (!groundContact.IsGrounded)
        {
            updatedVelocity.y = rb.velocity.y;
        }

        rb.velocity = updatedVelocity;
    }

    private float ChooseAcceleration(float moveInput, float desiredVelocityX)
    {
        // Case 1) 이동을 멈추는 경우
        bool isStopping = moveInput == 0f;
        if (isStopping)
        {
            return moveDecceleration;
        }

        // Case 2) 반대 방향으로 이동하려는 경우
        bool isTurningDirection = rb.velocity.x * desiredVelocityX < 0f;
        if (isTurningDirection)
        {
            return turnAcceleration;
        }

        // Case 3) 기존 방향으로 계속 이동하는 경우
        return moveAcceleration;
    }

    private void HandleJumpInput()
    {
        // 공격, 경직 등 다른 상태에서는 점프 불가능
        if (ActionState == PlayerActionState.IdleOrRun || ActionState == PlayerActionState.StickToWall)
        {
            // 점프에는 두 가지 경우가 있음
            // 1. 1차 점프 - 플랫폼과 접촉한 경우 또는 coyote time이 아직 유효한 경우
            // 2. 2차 점프 - 이미 점프한 경우 또는 coyote time이 유효하지 않은 경우
            if (IsInitialJump())
            {
                jumpCount = 1;
                PerformJump();
            }
            else if (IsDoubleJump())
            {
                // TODO: 더블 점프는 로그 타입만 가능하도록 수정
                jumpCount = 2;
                PerformJump();
            }
        }

        // 입력 처리 완료
        isJumpKeyPressed = false;
    }

    // 최초의 점프로 취급하는 경우
    // 1. 바닥에 서있는 경우
    // 2. 벽에 매달려있는 경우
    // 3. 1또는 2의 상황에서 추락을 시작한지 얼마 지나지 않은 경우 (coyote time 유효)
    private bool IsInitialJump()
    {
        return jumpCount == 0 && coyoteTimeCounter < coyoteTime;
    }

    // 더블 점프로 취급하는 경우
    // 1. 이미 최초의 점프를 완료한 경우
    // 2. 아직 점프를 하지는 않았지만 추락 시간이 허용된 coyote time을
    //    초과해서 공중에 떠있는 것으로 취급하는 경우
    private bool IsDoubleJump()
    {
        // 더블 점프는 일단 폐기...
        return false;
        //return jumpCount == 1 || (jumpCount == 0 && coyoteTimeCounter > coyoteTime);
    }

    private void PerformJump()
    {
        // 지금 벽에 매달려있거나 방금까지 벽에 매달려있던 경우 (coyote time) wall jump로 전환
        if (shouldWallJump)
        {
            // 더블 점프에서도 wall jump가 실행되는 것 방지
            shouldWallJump = false;

            StopStickingToWall();

            ApplyWallJumpAirControlForDurationAsync(wallJumpAirControlPenaltyDuration).Forget();

            // wallJumpVelocity는 오른쪽으로 박차고 나가는 기준이라서
            // 왼쪽으로 가야 하는 경우 x축 속도를 반전시켜야 함.
            rb.velocity = new(wallJumpVelocity.x * (isStickingToRightWall ? -1f : 1f), wallJumpVelocity.y);

            // 점프하는 방향 바라보기
            IsFacingLeft = rb.velocity.x < 0f;
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
        }

        // Coyote time에 점프한 경우 중력이 gravityScaleWhenFalling으로
        // 설정되어 있으므로 점프 시 중력으로 덮어쓰기.
        rb.gravityScale = defaultGravityScale;
        
        // 경사로를 타고 올라가다가 점프하면 바로 착지해버리는 상황을
        // 잠시동안 지면 검출을 멈추는 방식으로 방지함.
        groundContact.PreventTestForDuration(0.1f);

        // 점프 애니메이션 재생
        animator.SetTrigger("Jump");
    }

    // wall jump 직후에 너무 빨리 벽으로 돌아오는 것을
    // 막기 위해 잠시 더 낮은 airControl 수치를 적용함.
    private async UniTask ApplyWallJumpAirControlForDurationAsync(float duration)
    {
        airControl = airControlWhileWallJumping;

        await UniTask.WaitForSeconds(duration);

        airControl = defaultAirControl;
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
        if (!groundContact.IsGrounded && ActionState != PlayerActionState.StickToWall)
        {
            newPosition.y = cameraFollowTarget.transform.position.y;
        }

        cameraFollowTarget.transform.position = newPosition;
    }

    // 매 프레임 갱신해야 하는 애니메이터 파라미터 관리
    private void UpdateAnimatorState()
    {
        animator.SetBool("IsGrounded", groundContact.IsGrounded);
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
        return ActionState == PlayerActionState.AttackInProgress || ActionState == PlayerActionState.AttackWaitingContinuation;
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
        rb.sharedMaterial = zeroFrictionMat;
        SetStaggerStateForDurationAsync(staggerDuration).Forget();

        // TODO:
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
            StopStickingToWall();
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

        await UniTask.WaitForSeconds(duration, cancellationToken: stunCancellation.Token);

        ActionState = PlayerActionState.IdleOrRun;
    }
}
