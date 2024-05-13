using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour, IDestructible
{
    [Header("Walk / Run")]
    [SerializeField] private float maxMoveSpeed = 1.5f;
    [SerializeField] private float turnAcceleration = 60f;
    [SerializeField] private float moveAcceleration = 30f;
    [SerializeField] private float moveDecceleration = 50f;


    [Header("Jump / Fall")]
    [SerializeField] private float jumpVelocity = 4f;
    [SerializeField] private Vector2 wallJumpVelocity = new(2f, 4f);
    // 땅에서 떨어져도 점프를 허용하는 time window
    [SerializeField] private float coyoteTime = 0.15f;
    // 공중에 있지만 위로 이동하는 중이라면 DefaultGravityScale을 사용하고
    // 아래로 이동하는 중이라면 GravityScaleWhenFalling을 사용해
    // 더 빨리 추락해서 공중에 붕 뜨는 이상한 느낌을 줄임.
    [SerializeField] private float defaultGravityScale = 1f;
    [SerializeField] private float gravityScaleWhileFalling = 1.7f;
    // 아주 높은 곳에서 떨어질 때 부담스러울 정도로 아래로 가속하는 상황 방지
    [SerializeField] private float maxFallSpeed = 5f;
    // 공중 조작이 지상 조작보다 둔하게 만드는 파라미터 (0: 조작 불가, 1: 변화 없음)
    [SerializeField, Range(0f, 1f)] private float defaultAirControl = 0.5f;
    // wall jump 이후 벽으로 돌아오는데에 걸리는 시간을 조정하는 파라미터 (airControl을 잠시 이 값으로 대체함)
    [SerializeField, Range(0f, 1f)] private float airControlWhileWallJumping = 0.2f;
    // wall jump 이후 defaultAirControl 대신 airControlWhileWallJumping을 적용할 기간
    [SerializeField, Range(0f, 1f)] private float wallJumpAirControlPenaltyDuration = 0.3f;
    // 최대 공중공격 콤보 횟수
    [SerializeField] private int maxOnAirAttackCount = 2;



    [Header("Ground Contact")]
    // 땅으로 취급할 layer를 모두 에디터에서 지정해줘야 함!
    [SerializeField] private LayerMask groundLayerMask;
    // 콜라이더로부터 이 거리보다 가까우면 접촉 중이라고 취급
    [SerializeField] private float contactDistanceThreshold = 0.02f;

    [Header("Follow Camera Target")]
    // Cinemachine Virtual Camera의 Follow로 등록된 오브젝트를 지정해줘야 함!
    // 새로운 높이의 플랫폼에 착지하기 전까지 카메라의 y축 좌표를 일정하게 유지하는 용도.
    [SerializeField] private GameObject cameraFollowTarget;
    // 바라보는 방향으로 얼마나 앞에 있는 지점을 카메라가 추적할 것인지
    [SerializeField, Range(0f, 2f)] private float cameraLookAheadDistance = 1f;

    [Header("Stagger")]
    // 경직을 주는 공격에 맞았을 때 얼마나 강하게 밀려날 것인지
    [SerializeField] private float weakStaggerKnockbackForce = 5f;
    [SerializeField] private float strongStaggerKnockbackForce = 8f;
    // 경직의 지속 시간
    [SerializeField] private float weakStaggerDuration = 0.2f;
    [SerializeField] private float strongStaggerDuration = 0.5f;

    [Header("Attack")]
    // 공격 범위로 사용할 콜라이더
    [SerializeField] private Collider2D weaponCollider;



    // 컴포넌트 레퍼런스
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerStat playerStat;
    private InteractionManager interactionManager;
    private FlashEffectOnHit flashEffectOnHit;

    // 입력 시스템
    private ModuleBerserkActionAssets actionAssets;
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
    private bool isFacingRight = true;
    // 벽에 메달린 방향이 오른쪽인지 기록.
    // 벽에서 떨어져야 하는지 테스트할 때 사용됨.
    private bool isStickingToRightWall;
    // defaultAirControl과 airControlWhileWallJumping 중 실제로 적용될 수치
    private float airControl;
    
    //Prototype 공격용 변수들
    private bool isAttackInputBuffered = false; // 공격 버튼 선입력 여부
    private bool isAirAttackPossible = true; // 공중 공격을 시작할 수 있는지
    private int attackCount = 0;
    private int maxAttackCount = 2; // 최대 연속 공격 횟수. attackCount가 이보다 커지면 첫 공격 모션으로 돌아감.
    private float prevSpritePivotX; // 공격 애니메이션에 따른 이동을 처리하기 위한 변수.

    // 경직 도중에 또 경직을 당하거나 긴급 회피로 탈출하는 경우 기존 경직 취소
    private CancellationTokenSource staggerCancellation = new();

    private enum State
    {
        IdleOrRun, // 서있기, 달리기, 점프, 낙하
        StickToWall, // 벽에 매달려 정지한 상태
        Stagger, // 공격에 맞아 경직 판정인 상태
        AttackInProgress, // 공격 모션의 선딜 ~ 후딜까지의 기간 (선입력 대기하는 중)
        AttackWaitingContinuation, // 선입력은 없었지만 언제든 공격 키를 눌러 다음 공격을 이어나갈 수 있는 상태
    };
    private State state = State.IdleOrRun;

    private void Awake()
    {
        FindComponentReferences();
        BindInputActions();
        
        groundContact = new(rb, boxCollider, groundLayerMask, contactDistanceThreshold);
        airControl = defaultAirControl;
    }

    private void Start()
    {
        // TODO: playerStat.HP.OnValueChange에 체력바 UI 업데이트 함수 등록
    }

    private void FindComponentReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        playerStat = GetComponent<PlayerStat>();
        interactionManager = GetComponent<InteractionManager>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
    }

    private void BindInputActions()
    {
        actionAssets = new ModuleBerserkActionAssets();
        actionAssets.Player.Enable();

        actionAssets.Player.Jump.performed += OnJump;
        actionAssets.Player.FallDown.performed += OnFallDown;
        actionAssets.Player.PerformAction.performed += OnPerformAction;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        isJumpKeyPressed = true;
    }

    private void OnFallDown(InputAction.CallbackContext context)
    {
        if (groundContact.IsSteppingOnOneWayPlatform())
        {
            groundContact.IgnoreCurrentPlatformForDurationAsync(0.5f).Forget();
        }
    }

    private void OnPerformAction(InputAction.CallbackContext context)
    {
        // 1순위 행동: 상호작용 (아이템 줍기, NPC와 대화)
        if (interactionManager.CanInteract)
        {
            interactionManager.StartInteractionWithLatestTarget();
        }
        // 2순위 행동: 공격
        else
        {
            // 경직 상태이거나 벽에 매달린 경우는 공격 불가
            if (state == State.Stagger || state == State.StickToWall)
            {
                return;
            }

            // 이미 핵심 공격 모션이 재생 중이라면 선입력으로 처리,
            // 아니라면 다음 공격 모션 재생
            if (state == State.AttackInProgress)
            {
                isAttackInputBuffered = true;
            }
            else
            {
                TriggerNextAttack();
            }
        }
    }

    private void TriggerNextAttack()
    {
        // 이미 공중 공격을 했으면 착지하기 전까지는 공격 불가
        if (!groundContact.IsGrounded && !isAirAttackPossible)
        {
            return;
        }

        // 공격을 시작하는 순간에 한해 방향 전환 허용
        UpdateFacingDirection(actionAssets.Player.Move.ReadValue<float>());

        // 공격 도중에는 공격 모션에 의한 약간의 이동을 제외한 모든 움직임이 멈춤
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        // 다음 공격 모션 선택
        if (attackCount < maxAttackCount)
        {
            attackCount++;
        }
        else
        {
            attackCount = 1;
        }

        // 공중에서는 최대 2회까지만 공격 가능
        if (attackCount >= maxOnAirAttackCount && !groundContact.IsGrounded)
        {
            isAirAttackPossible = false;
        }

        // note:
        // 트리거 이름은 Attack1부터 시작하며,
        // 각각의 공격 애니메이션은 네 가지 이벤트를 제공해야 함
        // 1. 선딜레이가 끝나고 공격 판정이 시작되는 시점 => OnEnableAttackCollider
        // 2. 타격 모션이 끝나고 후딜레이가 시작되는 시점 => OnDisableAttackCollider
        // 3. 선입력에 의해 자동으로 공격을 이어나가는 시점 => OnStartWaitingAttackContinuation
        // 4. 공격 모션이 완전히 끝난 뒤 => OnAttackMotionEnd
        //
        // TODO: 만약 공격 모션마다 히트박스가 달라지는 경우 처리 방식 수정하기
        animator.SetTrigger($"Attack{attackCount}");

        // 공격 애니메이션의 pivot 변화로 루트모션을
        // 적용하기 때문에 시작할 때 기준점을 잡아줘야 함.
        prevSpritePivotX = spriteRenderer.sprite.pivot.x;

        state = State.AttackInProgress;
    }

    public void OnEnableAttackCollider()
    {
        weaponCollider.enabled = true;

        // 바라보는 방향에 따라 콜라이더 위치 조정
        float newOffsetX = Mathf.Abs(weaponCollider.offset.x) * (isFacingRight ? 1f : -1f);
        weaponCollider.offset = new Vector2(newOffsetX, weaponCollider.offset.y);
    }

    public void OnDisableAttackCollider()
    {
        weaponCollider.enabled = false;
    }

    // 공격 애니메이션이 완전히 종료되는 시점에 호출되는 이벤트.
    // 공격 상태를 종료하고 IdleOrRun 상태로 복귀함.
    public void OnAttackMotionEnd()
    {
        CancelCurrentAction();
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
            state = State.AttackWaitingContinuation;
        }
    }

    // UI가 뜨거나 컷신에 진입하는 등 잠시 입력을 막아야 하는 경우 사용
    public void SetInputEnabled(bool enable)
    {
        if (enable)
        {
            actionAssets.Player.Enable();
        }
        else
        {
            actionAssets.Player.Disable();
        }
    }

    private void FixedUpdate()
    {
        // one way platform을 위로 스쳐 지나가는 상황에서
        // 공격 상태에 진입해 정지하면 IsGrounded가 true가 되어버림.
        // 실제로는 공중에 떠 있는 것으로 취급해야 하므로 공격 중이 아닐 때만 상태를 갱신함.
        if (!IsAttacking())
        {
            groundContact.TestContact();
            if (groundContact.IsGrounded)
            {
                ResetJumpRelatedStates();
            }
            else if (state == State.IdleOrRun)
            {
                HandleCoyoteTime();
                HandleFallingVelocity();
            }
        }
        // 공격 중이라면 애니메이션의 pivot 변화에 따라 움직임을 부여 (일종의 루트 모션)
        // TODO: 그냥 animator에 apply root motion 체크해도 같은 효과가 나오는지 확인하기
        else
        {
            ApplyAttackRootMotion();
        }

        HandleMoveInput();

        if (isJumpKeyPressed)
        {
            HandleJumpInput();
        }

        if (state == State.Stagger)
        {
            // 넉백 효과로 생긴 velocity 부드럽게 감소
            UpdateMoveVelocity(0f);
        }

        UpdateCameraFollowTarget();
        UpdateSpriteDirection();
        UpdateAnimatorState();
    }

    private void ResetJumpRelatedStates()
    {
        jumpCount = 0;
        isAirAttackPossible = true;
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

    // 원본 공격 애니메이션들을 보면 플레이어가 중심 위치에
    // 가만히 있지 않고 pivot을 기준으로 조금씩 이동함.
    // 이걸 그냥 쓰면 플레이어 오브젝트는 가만히 있는데
    // 스프라이트만 이동하는 것처럼 보이므로 굉장히 이상해짐...
    //
    // 지금 사용하는 방식:
    // 1. 공격 애니메이션의 프레임 별 pivot이 항상 플레이어의 중심에 오도록 수정
    //    => 이제 애니메이션 재생해도 플레이어는 제자리에 있는 것처럼 보임
    // 2. 스프라이트 상의 이동을 실제 플레이어 오브젝트의 이동으로 변환하기 위해 pivot 변화량을 계산
    //    => pivot 변화량에 비례해 velocity를 부여해서 원본 에셋의 이동하는 느낌을 물리적으로 재현
    private void ApplyAttackRootMotion()
    {
        // 스프라이트의 pivot이 커졌다는 것은 플레이어의 중심 위치가
        // 오른쪽으로 이동했다는 뜻이므로 오른쪽 방향으로 속도를 주면 됨.
        // 서로 다른 공격 모션 사이에서는 가끔 음수가 나오는 이상한 현상이 있어서 max를 취함.
        float currSpritePivotX = spriteRenderer.sprite.pivot.x;
        float rootMotion = Mathf.Max(currSpritePivotX - prevSpritePivotX, 0f);

        // 스프라이트는 항상 오른쪽만 바라보니까 루트 모션도 항상 오른쪽으로만 나옴.
        // 실제 바라보는 방향으로 이동할 수 있도록 왼쪽 또는 오른쪽 벡터를 선택함.
        // 마지막에 곱하는 0.5은 원본 애니메이션과 비슷한 이동 거리가 나오도록 실험적으로 구한 수치.
        rb.velocity = (isFacingRight ? Vector2.right : Vector2.left) * rootMotion * 0.5f;

        prevSpritePivotX = currSpritePivotX;
    }

    private void HandleMoveInput()
    {
        float moveInput = actionAssets.Player.Move.ReadValue<float>();
        
        if (state == State.IdleOrRun)
        {
            UpdateFacingDirection(moveInput);
            if (ShouldStickToWall(moveInput))
            {
                StartStickingToWall(moveInput);
            }
            else
            {
                UpdateMoveVelocity(moveInput);
            }

        }
        else if (state == State.StickToWall && ShouldStopStickingToWall(moveInput))
        {
            StopStickingToWall();
        }
    }

    private void UpdateFacingDirection(float moveInput)
    {
        if (moveInput != 0f)
        {
            isFacingRight = moveInput > 0f;
        }
    }

    // 공중에 있고 이동하려는 방향의 벽과 접촉한 경우에 한해 true 반환.
    private bool ShouldStickToWall(float moveInput)
    {
        bool shouldStickRight = moveInput > 0f && groundContact.IsInContactWithRightWall;
        bool shouldStickLeft = moveInput < 0f && groundContact.IsInContactWithLeftWall;
        return !groundContact.IsGrounded && (shouldStickRight || shouldStickLeft);
    }

    // 벽에 매달린 상태에서 입력을 취소하거나 반대 방향으로 이동하려는 경우 true 반환.
    private bool ShouldStopStickingToWall(float moveInput)
    {
        return (isFacingRight != isStickingToRightWall) || (moveInput == 0f);
    }

    private void StartStickingToWall(float moveInput)
    {
        state = State.StickToWall;

        // 매달린 방향과 반대로 이동하는 경우 매달리기 취소해야 하므로 현재 방향 기록
        isStickingToRightWall = moveInput > 0f;

        // wall jump 가능하게 설정
        jumpCount = 0;
        shouldWallJump = true;

        // coyote time 리셋
        coyoteTimeCounter = 0f;

        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;

        // TODO:
        // 1. 벽에 달라붙는 애니메이션으로 전환
        // 2. 벽에 붙어도 공중 공격 가능 여부를 초기화해야 한다면 isAirAttackPossible = true 넣기
    }

    private void StopStickingToWall()
    {
        state = State.IdleOrRun;

        // 중력 활성화
        rb.gravityScale = defaultGravityScale;
    }

    private void UpdateMoveVelocity(float moveInput)
    {
        // 원하는 속도를 계산
        float desiredVelocityX = maxMoveSpeed * moveInput * maxMoveSpeed;

        // 방향 전환 여부에 따라 다른 가속도 사용
        float acceleration = ChooseAcceleration(moveInput, desiredVelocityX);

        // 공중이라면 AirControl 수치(0.0 ~ 1.0)에 비례해 가속도 감소
        if (!groundContact.IsGrounded)
        {
            acceleration *= airControl;
        }

        // x축 속도가 원하는 속도에 부드럽게 도달하도록 보간
        float updatedVelocityX = Mathf.MoveTowards(rb.velocity.x, desiredVelocityX, acceleration * Time.deltaTime);
        rb.velocity = new Vector2(updatedVelocityX, rb.velocity.y);
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
        if (state == State.IdleOrRun || state == State.StickToWall)
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
                // TODO: 만약 연료가 부족하다면 더블 점프 방지하고, 충분하다면 연료 소모
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
        return jumpCount == 1 || (jumpCount == 0 && coyoteTimeCounter > coyoteTime);
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
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
        }

        // Coyote time에 점프한 경우 중력이 gravityScaleWhenFalling으로
        // 설정되어 있으므로 점프 시 중력으로 덮어쓰기.
        rb.gravityScale = defaultGravityScale;

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
        newPosition.x += isFacingRight ? cameraLookAheadDistance : -cameraLookAheadDistance;

        // 벽에 매달리거나 새로운 플랫폼에 착지하지 않았다면 y 좌표는 유지.
        if (!groundContact.IsGrounded && state != State.StickToWall)
        {
            newPosition.y = cameraFollowTarget.transform.position.y;
        }

        cameraFollowTarget.transform.position = newPosition;
    }

    private void UpdateSpriteDirection()
    {
        spriteRenderer.flipX = !isFacingRight;
    }

    // 매 프레임 갱신해야 하는 애니메이터 파라미터 관리
    private void UpdateAnimatorState()
    {
        animator.SetBool("IsGrounded", groundContact.IsGrounded);
        animator.SetFloat("HorizontalVelocity", rb.velocity.y);
        animator.SetBool("IsRunning", actionAssets.Player.Move.IsPressed()); //공격중 애니메이션 재생 ㄴㄴ
        animator.SetBool("IsAttacking", IsAttacking());
        animator.SetBool("IsStaggered", state == State.Stagger);
    }

    private bool IsAttacking()
    {
        return state == State.AttackInProgress || state == State.AttackWaitingContinuation;
    }

    CharacterStat IDestructible.GetHPStat()
    {
        return playerStat.HP;
    }

    CharacterStat IDestructible.GetDefenseStat()
    {
        return playerStat.Defense;
    }

    Team IDestructible.GetTeam()
    {
        return Team.Player;
    }

    void IDestructible.OnDamage(float finalDamage, StaggerInfo staggerInfo)
    {
        Debug.Log($"아야! 내 현재 체력: {playerStat.HP.CurrentValue}");

        flashEffectOnHit.StartEffectAsync().Forget();

        // TODO:
        // 지금은 데미지 입히는 타이밍에 제한이 없어서
        // ApplyDamageOnContact 스크립트가 붙은 오브젝트 둘
        // 사이에 끼어버리면 핀볼처럼 튕겨다니는 상황이 발생함.
        // 데미지를 입으면 아주 짧은 시간 무적 판정을 줘도 좋을 것 같음.
        // ex) 메이플스토리
        switch(staggerInfo.strength)
        {
            case StaggerStrength.Weak:
                ApplyStagger(staggerInfo.direction * weakStaggerKnockbackForce, weakStaggerDuration);
                break;
            case StaggerStrength.Strong:
                ApplyStagger(staggerInfo.direction * strongStaggerKnockbackForce, strongStaggerDuration);
                break;
        }
    }

    // 현재 하던 행동을 취소하고 피격 경직 상태에 진입
    private void ApplyStagger(Vector2 staggerForce, float staggerDuration)
    {
        CancelCurrentAction();

        rb.velocity = staggerForce;
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
        // TODO: 공격 구현할 때 여기에 공격 취소하는 로직도 추가할 것 (슈퍼아머 아닌 경우!)
        if (state == State.StickToWall)
        {
            StopStickingToWall();
        }
        else if (state == State.Stagger)
        {
            // CancellationTokenSource는 리셋이 불가능해서
            // 한 번 cancel하면 새로 만들어줘야 함.
            staggerCancellation.Cancel();
            staggerCancellation.Dispose();
            staggerCancellation = new();
        }
        else if (IsAttacking())
        {
            attackCount = 0;
            isAttackInputBuffered = false;
            rb.gravityScale = defaultGravityScale;
            OnDisableAttackCollider();

            // 만약 공중 공격이었다면 설령 maxAirAttackCount만큼
            // 연속 공격을 하지 않았더라도 착지하기 전까지 공격을 금지함.
            // 공중 공격은 점프 당 1회, 최대 maxAirAttackCount만큼 연격.
            if (!groundContact.IsGrounded)
            {
                isAirAttackPossible = false;
            }
        }

        state = State.IdleOrRun;
    }

    private async UniTask SetStaggerStateForDurationAsync(float duration)
    {
        state = State.Stagger;

        await UniTask.WaitForSeconds(duration, cancellationToken: staggerCancellation.Token);

        state = State.IdleOrRun;
    }

    void IDestructible.OnDestruction()
    {
        // TODO: 캐릭터 destroy & 은신처로 복귀
        Debug.Log("플레이어 사망");
    }
}
