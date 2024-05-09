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
    private Transform tempWeapon; //Prototype용 임시
    private bool isAttacking = false;
    private int airAttackCount = 0;

    // 경직 도중에 또 경직을 당하거나 긴급 회피로 탈출하는 경우 기존 경직 취소
    private CancellationTokenSource staggerCancellation = new();

    private enum State
    {
        IdleOrRun, // 서있기, 달리기, 점프, 낙하
        StickToWall, // 벽에 매달려 정지한 상태
        Stagger, // 공격에 맞아 경직 판정인 상태
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
        tempWeapon = transform.GetChild(0);
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
        if (interactionManager.CanInteract)
        {
            interactionManager.StartInteractionWithLatestTarget();
        }
        else
        {
            if (isAttacking) { // 임시로 이렇게 처리해놨습니당
                return;
            }
            if (airAttackCount >= maxOnAirAttackCount && !groundContact.IsGrounded) {
                return;
            }
            if (state != State.IdleOrRun) { //경직 및 벽 붙어서 공격 안됨
                return;
            }
            airAttackCount++; // 공중공격 횟수
            if (spriteRenderer.flipX){
                StartCoroutine(TempAttackMotion(1)); //-1 왼쪽, 1 오른쪽
            }
            else {
                StartCoroutine(TempAttackMotion(-1)); //-1 왼쪽, 1 오른쪽
            }
        }
    }

    IEnumerator TempAttackMotion(int direction) { //임시용        
    //솔직히 WeaponManager 만들어서 할까 했는데
    //너무 내 맘대로 막하는 거 같아서 걍 일단 이렇게 해봄
        isAttacking = true;
        tempWeapon.GetComponent<BoxCollider2D>().enabled = true;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(rb.velocity.x, 0);
        Vector3 pivot;
        float elapsedTime = 0f;
        while (elapsedTime < 0.25f) { // 무기 내려감
            pivot = transform.position - new Vector3 (0, tempWeapon.localScale.y * 0.3f, 0);
            // 무기 회전
            tempWeapon.transform.RotateAround(pivot, Vector3.forward, direction * 90f * Time.deltaTime * 4);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 이번엔 반대로 회전
        elapsedTime = 0f;
        while (elapsedTime < 0.25f) { // 무기 올라감
            pivot = transform.position - new Vector3 (0, tempWeapon.localScale.y * 0.3f, 0);
            // 초기 위치로 다시 회전
            tempWeapon.transform.RotateAround(pivot, Vector3.forward, direction * -90f * Time.deltaTime * 4);

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        isAttacking = false;
        tempWeapon.GetComponent<BoxCollider2D>().enabled = false;
        rb.gravityScale = 1.7f;
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
        groundContact.TestContact();
        if (groundContact.IsGrounded)
        {
            ResetJumpRelatedStates();
            airAttackCount = 0;
        }
        else if (state == State.IdleOrRun)
        {
            HandleCoyoteTime();
            HandleFallingVelocity();
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
        if (isFalling && !isAttacking) //공격 중에 추락 ㄴㄴ
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
        float moveInput;
        if (!isAttacking) {
            moveInput = actionAssets.Player.Move.ReadValue<float>();
        }
        else {
            moveInput = 0f;
        }
        

        // TODO: 공격 도중에는 방향 못 바꾸게 막기 (다음 공격 모션 직전에는 방향 전환 OK)
        UpdateFacingDirection(moveInput);
        

        if (state == State.IdleOrRun)
        {
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

        // TODO: 벽에 달라붙는 애니메이션으로 전환
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
        if (isAttacking) { //공격시 점프 불가
            isJumpKeyPressed = false; //선입력 방지
            return;
        }
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
        animator.SetBool("IsRunning", actionAssets.Player.Move.IsPressed() && !isAttacking); //공격중 애니메이션 재생 ㄴㄴ
        animator.SetBool("IsAttacking", isAttacking);
        
        animator.SetBool("IsStaggered", state == State.Stagger);
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

    // 경직에 걸리거나 기절당하는 등 현재 하던 행동을 종료해야 하는 경우 사용
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
