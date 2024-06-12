using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

// 순찰, 추적 등 잡몹들이 공통적으로 공유하는 행동 패턴을 구현하는 클래스.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public abstract class EnemyBehaviorBase : MonoBehaviour, IEnemyBehavior
{
    [Header("Chase")]
    // Chase 상태에서 이동을 멈추기 위한 플레이어와의 거리 조건.
    // 거리가 min과 max 사이에 있는 경우에만 추적을 시도한다.
    [SerializeField] private float chaseMinDistance = 0.5f;
    [SerializeField] private float chaseMaxDistance = 5f;
    [SerializeField] private float chaseSpeed = 1f;


    [Header("Ground Contact")]
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float contactDistanceThreshold = 0.02f;


    [Header("Patrol")]
    // 순찰 하위 상태인 '걷기' 또는 '대기'의 지속시간 범위.
    // 하위 상태가 변경될 때마다 min ~ max 사이의 랜덤한 시간이 할당된다.
    [SerializeField] private float minPatrolSubbehaviorDuration = 1f;
    [SerializeField] private float maxPatrolSubbehaviorDuration = 4f;
    // 순찰 중 걷기 상태의 이동 속도
    [SerializeField] private float patrolSpeed = 1f;


    [Header("Stagger")]
    [SerializeField] private float weakStaggerForce = 5f;
    [SerializeField] private float strongStaggerForce = 10f;
    [SerializeField] private float knockbackDecceleration = 30f;


    [Header("Stat Randomization")]
    // 이동 속도, 공격 딜레이 등 각종 수치를 몹마다 다르게 할 비율 (퍼센트 단위, 0이면 랜덤성 없음)
    [SerializeField] protected float randomizationFactor = 0.1f;

    public bool IsFacingLeft
    {
        get => spriteRenderer.flipX;
        protected set => spriteRenderer.flipX = value;
    }

    // 컴포넌트 레퍼런스
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;
    protected BoxCollider2D boxCollider;

    // 플레이어 추적용 레퍼런스
    protected GameObject player;

    // 플레이어를 추적할 때 낙하하지 않을 상태인지 확인하기 위해 사용
    protected GroundContact groundContact;

    
    // 현재 순찰 상태인지 나타내는 플래그
    private bool isPatrolling = false;
    // 연속적으로 같은 순찰 방향이 선택된 횟수
    // 방향이 바뀌면 1부터 시작함
    private int samePatrolDirectionCount = 0;
    // 순찰 세부 상태 중 '걷기' 또는 '대기'가 유지된 시간
    private float remaningPatrolSubbehaviorDuration = 0f;
    // 순찰 세부 상태
    private enum PatrolSubbehavior
    {
        Walk,
        Pause,
    }
    private PatrolSubbehavior patrolSubbehavior = PatrolSubbehavior.Pause;

    // 현재 경직 상태인지 확인하기 위한 플래그.
    // 넉백 효과를 부드럽게 감소시킬 때에도 사용한다.
    private bool isStaggered = false;
    private CancellationTokenSource staggerCancellation = new();

    protected void Awake()
    {
        FindComponentReferences();
        RandomizeSpeedStats();

        groundContact = new(rb, boxCollider, groundLayerMask, contactDistanceThreshold);
    }

    protected void FixedUpdate()
    {
        groundContact.TestContact();

        animator.SetBool("IsMoving", Mathf.Abs(rb.velocity.x) > 0.01f);

        // 순찰 상태
        if (isPatrolling)
        {
            PerformPatrol();
        }

        // 경직과 같이 부여된 넉백 효과 부드럽게 감소
        if (isStaggered)
        {
            float updatedVelocityX = Mathf.MoveTowards(rb.velocity.x, 0f, knockbackDecceleration * Time.fixedDeltaTime);
            rb.velocity = new Vector2(updatedVelocityX, rb.velocity.y);
        }
    }

    private void FindComponentReferences()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // 움직임 관련 수치들을 약간씩 바꿔서 적들이 같은 위치에 겹치는 것을 방지
    private void RandomizeSpeedStats()
    {
        chaseMinDistance *= SampleRandomizationFactor();
        chaseSpeed *= SampleRandomizationFactor();
        patrolSpeed *= SampleRandomizationFactor();
    }

    protected float SampleRandomizationFactor()
    {
        return Random.Range(1 + randomizationFactor, 1 - randomizationFactor);
    }

    // 주어진 방향이 낭떠러지인지 반환
    protected bool IsOnBrink(float direction)
    {
        return 
            (direction > 0f && !groundContact.IsRightFootGrounded) ||
            (direction < 0f && !groundContact.IsLeftFootGrounded);
    }

    void IEnemyBehavior.StartPatrol()
    {
        isPatrolling = true;
        InitializePatrolState();
    }

    void IEnemyBehavior.StopPatrol()
    {
        isPatrolling = false;
    }

    protected void InitializePatrolState()
    {
        samePatrolDirectionCount = 0;
        SetPatrolSubbehavior(PatrolSubbehavior.Pause);
    }

    private void SetPatrolSubbehavior(PatrolSubbehavior subbehavior)
    {
        patrolSubbehavior = subbehavior;

        // 이번 하위 상태의 지속시간을 랜덤하게 할당
        remaningPatrolSubbehaviorDuration = Random.Range(minPatrolSubbehaviorDuration, maxPatrolSubbehaviorDuration);

        // '걷기' 상태에 돌입한 경우 방향 전환
        if (patrolSubbehavior == PatrolSubbehavior.Walk)
        {
            ChooseRandomPatrolDirection();
        }
    }

    // 낭떠러지 방향이 아닌 랜덤한 방향으로 순찰을 진행
    private void ChooseRandomPatrolDirection()
    {
        float prevPatrolSpeed = patrolSpeed;

        // 50% 확률로 랜덤하게 순찰 방향을 선택
        // int버전 Random은 최대치가 exclusive라서 2를 줘야 0 또는 1이 나옴!
        patrolSpeed = Mathf.Abs(patrolSpeed) * (Random.Range(0, 2) == 0 ? 1f : -1f);

        // 순찰을 시작한 뒤로 처음 방향을 정하는 경우,
        // 또는 이전 순찰 방향과 동일한 방향이 걸린 경우 카운터 증가
        if (samePatrolDirectionCount == 0 || patrolSpeed == prevPatrolSpeed)
        {
            ++samePatrolDirectionCount;
        }

        // 만약 같은 순찰 방향이 3회 이상 걸렸거나 해당 방향이 낭떠러지인 경우 반대 방향 선택
        if (samePatrolDirectionCount >= 3 || IsOnBrink(patrolSpeed))
        {
            patrolSpeed *= -1f;
            samePatrolDirectionCount = 1;
        }
    }

    protected void PerformPatrol()
    {
        // 아직 순찰이 끝나지 않은 경우 '걷기'와 '대기'라는 순찰 하위 상태를 반복함
        UpdatePatrolSubbehavior();

        if (patrolSubbehavior == PatrolSubbehavior.Walk)
        {
            PatrolWalk();
        }
        else
        {
            PatrolPause();
        }
    }

    private void UpdatePatrolSubbehavior()
    {
        // '걷기' 또는 '대기'가 지속된 시간 누적
        remaningPatrolSubbehaviorDuration -= Time.fixedDeltaTime;

        // 하위 상태 지속 시간이 끝나면 '걷기'와 '대기'를 번갈아가며 실행
        if (remaningPatrolSubbehaviorDuration < 0f)
        {
            if (patrolSubbehavior == PatrolSubbehavior.Walk)
            {
                SetPatrolSubbehavior(PatrolSubbehavior.Pause);
            }
            else
            {
                SetPatrolSubbehavior(PatrolSubbehavior.Walk);
            }
        }
    }

    // 순찰 하위 상태 중 '걷기'에 해당하는 행동
    private void PatrolWalk()
    {
        // 순찰 방향 바라보기
        IsFacingLeft = patrolSpeed < 0f;

        // 해당 방향이 낭떠러지가 아니라면 이동
        if (!IsOnBrink(patrolSpeed))
        {
            rb.velocity = new Vector2(patrolSpeed, rb.velocity.y);
        }
    }

    // 순찰 하위 상태 중 '대기'에 해당하는 행동
    private void PatrolPause()
    {
        rb.velocity = new Vector2(0f, rb.velocity.y);
    }

    void IEnemyBehavior.Chase()
    {
        Assert.IsNotNull(player);

        // 플레이어와의 x축 좌표 차이
        float displacement = player.transform.position.x - transform.position.x;

        // 플레이어 방향으로 스프라이트 설정
        IsFacingLeft = displacement < 0f;

        // 아직 멈춰도 될만큼 가깝지 않다면 계속 이동
        if (Mathf.Abs(displacement) > chaseMinDistance)
        {
            rb.velocity = new Vector2(Mathf.Sign(displacement) * chaseSpeed, rb.velocity.y);
        }
    }

    bool IEnemyBehavior.CanChasePlayer()
    {
        // 플레이어어가 존재하지 않는 경우
        if (!player)
        {
            return false;
        }

        // 플레이어가 있는 방향이 낭떠러지인 경우
        Vector2 chaseDirection = player.transform.position - transform.position;
        if (IsOnBrink(chaseDirection.x))
        {
            return false;
        }

        // 플레이어가 추적 가능 범위를 벗어난 경우
        //
        // TODO:
        // 일부 적은 플레이어와의 거리가 아니라 특정 영역 내부를 최우선으로 지키기도 함,
        // 다른 Behavior 클래스를 만들던지, 여기에 플래그로 if-else 처리를 하던지 해야 함
        if (chaseDirection.magnitude > chaseMaxDistance)
        {
            return false;
        }

        return true;
    }

    bool IEnemyBehavior.IsStaggerFinished()
    {
        return !isStaggered;
    }

    // 슈퍼아머 판정은 적 종류별로 다를 수 있어서 여기서 직접 처리하지는 않음.
    // 만약 경직 및 넉백이 필요하다면 GetStaggeredForDuration()을 사용하면 된다.
    public abstract bool TryApplyStagger(StaggerInfo staggerInfo);

    // 넉백 효과를 부여하고 잠시 경직 상태를 부여.
    // 애니메이션이나 이펙트같은 side effect는 처리해주지 않는다!
    //
    // 효과:
    // 1. 지속 시간 동안 IsStaggerFinished()가 false를 반환
    // 2. 넉백 효과 부여 (속도는 부드럽게 감소)
    // 3. 넉백 당한 방향 바라보기
    // 4. 순찰 상태 취소
    //
    // * 순찰은 IEnemyBehavior 중에서 유일하게 stateful한 행동이라서
    //   수동으로 취소해주지 않으면 FixedUpdate()에서 계속 순찰을 시도해버림!
    protected async UniTask GetStaggeredForDuration(StaggerInfo staggerInfo)
    {
        // 아직 경직이 끝나지 않은 경우 기존 task를 취소해서
        // 도중에 isStaggered = false가 되어버리는 것을 막아야 함
        if (isStaggered)
        {
            staggerCancellation.Cancel();
            staggerCancellation.Dispose();
            staggerCancellation = new CancellationTokenSource();
        }

        // 공격받은 방향 바라보기 (오른쪽으로 넉백 <=> 왼쪽에서 공격당함)
        IsFacingLeft = staggerInfo.direction.x > 0f;

        // 넉백 효과
        rb.AddForce(staggerInfo.direction * GetKnockbackForce(staggerInfo.strength), ForceMode2D.Impulse);

        // 잠시 경직 상태에 돌입
        isPatrolling = false;
        isStaggered = true;

        // 기존 경직이 끝나기 전에 새로운 경직 효과가 부여되는 경우
        // isStaggered를 true 상태로 유지한 채 바로 종료해야 함.
        var cancellationToken = staggerCancellation.Token;
        await UniTask.WaitForSeconds(staggerInfo.duration, cancellationToken: cancellationToken);

        if (!cancellationToken.IsCancellationRequested)
        {
            isStaggered = false;
        }
    }

    // 경직 종류별 넉백 impulse 크기
    private float GetKnockbackForce(StaggerStrength staggerStrength)
    {
        if (staggerStrength == StaggerStrength.Weak)
        {
            return weakStaggerForce;
        }
        else if (staggerStrength == StaggerStrength.Strong)
        {
            return strongStaggerForce;
        }
        else
        {
            return 0f; // StaggerStrength.None
        }
    }
}