using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 아주 단순한 근접 공격 잡몹의 행동을 정의함:
// 1. 자신이 서있는 플랫폼 위에 한해 주인공을 추적
// 2. 근접 공격 모션이 하나만 존재
// 3. 대기 모션이 두 가지 존재하며 반복 재생 횟수가
//    일정 수치를 넘어가면 둘 중에서 다른 모션으로 전환됨.
//
// 의존성:
// 1. Animation Controller
//    - 대기 모션의 마지막 프레임에 OnIdleAnimationEnd 이벤트 필요
//    - ChangeIdleAnimation 트리거에 반응해 두 종류의 대기 모션을 번갈아가며 사용해야 함
//    - 공격 모션의 마지막 프레임에 OnAttackMotionEnd 이벤트 필요
//
// Note:
// 지금으로서는 대부분의 근거리 적이 거의 같은 행동을 보일 것으로 예상되므로
// 적 종류별로 생기는 소소한 차이점은 이 클래스를 상속받아 IEnemyBehavior의
// 함수 일부를 override한 EnemyBehavior를 사용하는 식으로 구현할 계획임.
// 만약 차이점이 너무 크다면 그냥 IMeleeEnemyBehavior를 새로 구현하면 됨.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class MeleeEnemyBehaviorBase : MonoBehaviour, IMeleeEnemyBehavior
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

    
    [Header("Melee Attack")]
    // 다음 공격까지 기다려야 하는 시간
    [SerializeField] private float delayBetweenMeleeAttacks = 3f;


    [Header("Patrol")]
    // 순찰 하위 상태인 '걷기' 또는 '대기'의 지속시간 범위.
    // 하위 상태가 변경될 때마다 min ~ max 사이의 랜덤한 시간이 할당된다.
    [SerializeField] private float minPatrolSubbehaviorDuration = 1f;
    [SerializeField] private float maxPatrolSubbehaviorDuration = 4f;
    // 순찰 중 걷기 상태의 이동 속도
    [SerializeField] private float patrolSpeed = 1f;


    [Header("Stat Randomization")]
    // 이동 속도, 공격 딜레이 등 각종 수치를 몹마다 다르게 할 비율 (퍼센트 단위, 0이면 랜덤성 없음)
    [SerializeField] private float randomizationFactor = 0.1f;


    // 컴포넌트 레퍼런스
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    // 플레이어 추적용 레퍼런스
    private GameObject player;

    // 플레이어를 추적할 때 낙하하지 않을 상태인지 확인하기 위해 사용
    private GroundContact groundContact;

    // 현재 대기 애니메이션이 반복 재생된 횟수
    private int idleAnimationRepetitionCount = 0;
    // 다른 대기 애니메이션으로 전환되기 위한 반복 재생 횟수
    private const int IDLE_ANIMATION_CHANGE_THRESHOLD = 6;

    // 근접 공격 쿨타임 (0이 되면 공격 가능)
    private float remainingMeleeAttackCooltime = 0f;

    // 지금 공격 애니메이션이 재생 중인지 확인하기 위한 플래그
    private bool isAttackMotionFinished = true;

    // 현재 경직 상태인지 확인하기 위한 플래그.
    // 넉백 효과를 부드럽게 감소시키기 위해 사용한다.
    private bool isStaggered = false;

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

    private void Awake()
    {
        FindComponentReferences();
        RandomizeSpeedStats();

        groundContact = new(rb, boxCollider, groundLayerMask, contactDistanceThreshold);
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

    private float SampleRandomizationFactor()
    {
        return Random.Range(1 + randomizationFactor, 1 - randomizationFactor);
    }

    private void FixedUpdate()
    {
        groundContact.TestContact();

        remainingMeleeAttackCooltime -= Time.fixedDeltaTime;

        animator.SetBool("IsMoving", Mathf.Abs(rb.velocity.x) > 0.01f);

        // 순찰 상태
        if (isPatrolling)
        {
            PerformPatrol();
        }

        // 경직과 같이 부여된 넉백 효과 부드럽게 감소
        if (isStaggered)
        {
            float updatedVelocityX = Mathf.MoveTowards(rb.velocity.x, 0f, 30f * Time.deltaTime);
            rb.velocity = new Vector2(updatedVelocityX, rb.velocity.y);
        }
    }

    public void OnAttackMotionEnd()
    {
        isAttackMotionFinished = true;
    }

    void IEnemyBehavior.Chase()
    {
        // 플레이어와의 x축 좌표 차이
        float displacement = player.transform.position.x - transform.position.x;

        // 플레이어 방향으로 스프라이트 설정
        spriteRenderer.flipX = displacement < 0f;

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
        Vector2 displacement = player.transform.position - transform.position;
        if (IsOnBrink(displacement.x))
        {
            return false;
        }

        // 플레이어가 추적 가능 범위를 벗어난 경우
        if (displacement.magnitude > chaseMaxDistance)
        {
            return false;
        }

        return true;
    }

    void IEnemyBehavior.StartPatrol()
    {
        isPatrolling = true;
        samePatrolDirectionCount = 0;
        SetPatrolSubbehavior(PatrolSubbehavior.Pause);
    }

    void IEnemyBehavior.StopPatrol()
    {
        isPatrolling = false;
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

    // 순찰 방향이 낭떠러지인지 반환
    private bool IsOnBrink(float direction)
    {
        return 
            (direction > 0f && !groundContact.IsRightFootGrounded) ||
            (direction < 0f && !groundContact.IsLeftFootGrounded);
    }

    private void PerformPatrol()
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
        spriteRenderer.flipX = patrolSpeed < 0f;

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

    bool IEnemyBehavior.TryApplyStagger(StaggerInfo staggerInfo)
    {
        GetStaggeredForDuration(staggerInfo).Forget();

        return true;
    }

    private async UniTask GetStaggeredForDuration(StaggerInfo staggerInfo)
    {
        // 공격 모션이 재생 중이었을 가능성이 있으니 안전하게 플래그 정리.
        isAttackMotionFinished = true;

        // 공격받은 방향 바라보기 (오른쪽으로 넉백 <=> 왼쪽에서 공격당함)
        spriteRenderer.flipX = staggerInfo.direction.x > 0f;

        // 넉백 효과
        rb.AddForce(staggerInfo.direction * 5.0f, ForceMode2D.Impulse);

        // 애니메이션 재생
        animator.SetTrigger("Stagger");

        // 잠시 경직 상태에 돌입
        isStaggered = true;

        await UniTask.WaitForSeconds(staggerInfo.duration);

        isStaggered = false;
    }

    bool IEnemyBehavior.IsStaggerFinished()
    {
        return !isStaggered;
    }

    void IMeleeEnemyBehavior.MeleeAttack()
    {
        // 공격 모션 시작
        //
        // TODO:
        // 1. 적 애니메이션 완성되면 animation controller 제대로 만들기...
        //    지금은 플레이어 애니메이션 복사해서 사용하는지라
        //    호출해야될 이벤트가 없다는 등의 에라 로그가 출력됨.
        //    모션이 잘 나오는지만 확인하기 위해 만든 임시 에셋이니 일단 무시합시다
        // 2. 공격 애니메이션에 슈퍼아머 토글, 무기 콜라이더 토글하는 이벤트 추가!
        animator.SetTrigger("MeleeAttack");

        // 약간의 랜덤성을 부여한 쿨타임 시작
        // 적들이 동일한 간격으로 공격하는 것을 방지해 조금 더 자연스럽게 느껴지도록 한다
        remainingMeleeAttackCooltime = delayBetweenMeleeAttacks * SampleRandomizationFactor();

        // 공격 애니메이션 재생 중
        isAttackMotionFinished = false;
    }

    bool IMeleeEnemyBehavior.IsMeleeAttackReady()
    {
        return remainingMeleeAttackCooltime <= 0f;
    }

    bool IMeleeEnemyBehavior.IsMeleeAttackMotionFinished()
    {
        return isAttackMotionFinished;
    }

    // 대기 애니메이션의 마지막 프레임에 호출되는 이벤트
    public void OnIdleAnimationEnd()
    {
        idleAnimationRepetitionCount++;

        // 일정 횟수 이상 반복되면 다른 대기 애니메이션을 사용하도록 만든다
        if (idleAnimationRepetitionCount > IDLE_ANIMATION_CHANGE_THRESHOLD)
        {
            animator.SetTrigger("ChangeIdleAnimation");
        }
    }
}
