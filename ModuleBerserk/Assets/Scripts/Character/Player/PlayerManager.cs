using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
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


    // 컴포넌트 레퍼런스
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private PlayerStat playerStat;

    // 입력 시스템
    private ModuleBerserkActionAssets actionAssets;
    // 지면 접촉 테스트 관리자
    private GroundContact groundContact;
    // 땅에서 떨어진 시점부터 Time.deltaTime을 누적하는 카운터로,
    // 이 값이 CoyoteTime보다 낮을 경우 isGrounded가 false여도 점프 가능.
    private float coyoteTimeCounter = 0f;
    // coyote time, 더블 점프 등을 모두 고려한 점프 가능 여부로,
    // FixedUpdate()에서 업데이트됨.
    private bool canJump = true;
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

    // 상호작용 범위에 들어온 IInteractable 목록 (ex. NPC, 드랍 아이템, ...)
    private List<IInteractable> availableInteractables = new();
    
    //Prototype 공격용 변수들
    private Transform tempWeapon; //Prototype용 임시
    private bool isAttacking = false;

    private enum State
    {
        IdleOrRun,
        StickToWall,
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
        playerStat.HP.OnValueChange.AddListener(HandleHPChange);
    }

    private void HandleHPChange(float hp)
    {
        Debug.Log($"아야! 내 현재 체력: {hp}");

        // TODO:
        // 1. 체력바 UI 업데이트
        // 2. 사망 처리
    }

    private void FindComponentReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerStat = GetComponent<PlayerStat>();
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
            // await 없이 비동기로 처리하기 위해 discard
            _ = groundContact.IgnoreCurrentPlatformForDurationAsync(0.5f);
        }
    }

    private void OnPerformAction(InputAction.CallbackContext context)
    {
        if (availableInteractables.Count > 0)
        {
            // 제일 마지막에 활성화된 대상 선택
            availableInteractables.Last().StartInteraction();
        }
        else
        {
            if (isAttacking) { // 임시로 이렇게 처리해놨습니당
                return;
            }
            if (spriteRenderer.flipX){
                StartCoroutine(TempAttackMotion(1)); //-1 왼쪽, 1 오른쪽
            }
            else {
                StartCoroutine(TempAttackMotion(-1)); //-1 왼쪽, 1 오른쪽
            }
        }
    }

    IEnumerator TempAttackMotion(int direction) { //임시용        
        isAttacking = true;
        tempWeapon.GetComponent<BoxCollider2D>().enabled = true;
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

        UpdateCameraFollowTarget();
        UpdateSpriteDirection();
    }

    private void ResetJumpRelatedStates()
    {
        canJump = true;
        coyoteTimeCounter = 0f;
        rb.gravityScale = defaultGravityScale;
    }

    private void HandleCoyoteTime()
    {
        coyoteTimeCounter += Time.fixedDeltaTime;
        if (coyoteTimeCounter > coyoteTime)
        {
            canJump = false;
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
        float moveInput = actionAssets.Player.Move.ReadValue<float>();
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
        canJump = true;

        rb.velocity = Vector2.zero;
        rb.gravityScale = 0f;

        // TODO: 벽에 달라붙는 애니메이션으로 전환
    }

    private void StopStickingToWall()
    {
        state = State.IdleOrRun;

        // 중력 복구하고 coyote time 시작
        rb.gravityScale = defaultGravityScale;
        coyoteTimeCounter = 0f;

        // TODO: 낙하 애니메이션으로 전환
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
        if (canJump)
        {
            // 더블 점프 방지
            canJump = false;

            // 지금 벽에 매달려있거나 방금까지 벽에 매달려있던 경우 (coyote time) wall jump로 전환
            if (state == State.StickToWall || !groundContact.IsGrounded)
            {
                StopStickingToWall();

                // 비동기로 처리하기 위해 await하지 않고 discard
                _ = ApplyWallJumpAirControlForDurationAsync(wallJumpAirControlPenaltyDuration);

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
        }

        // 입력 처리 완료
        isJumpKeyPressed = false;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.TryGetComponent(out IInteractable interactable))
        {
            interactable.OnPlayerEnter();
            availableInteractables.Add(interactable);
        }
        // 일단 적이랑 충돌했을시 데미지 받는걸로 가정함
        else if (other.gameObject.TryGetComponent(out EnemyStat enemy))
        {
            if (!isAttacking) // 이것도 대충 처리해놈;
            {
                playerStat.HP.ModifyBaseValue(-enemy.AttackDamage.CurrentValue);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.TryGetComponent(out IInteractable interactable))
        {
            interactable.OnPlayerExit();
            availableInteractables.Remove(interactable);
        }
    }
}
