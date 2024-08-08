using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IEnemyPatrolBehavior))]
[RequireComponent(typeof(IEnemyChaseBehavior))]
[RequireComponent(typeof(IEnemyAttackBehavior))]
[RequireComponent(typeof(PlatformerMovement))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class NewEnemyController : MonoBehaviour
{
    [SerializeField] private PlayerDetectionRange playerDetectionRange;
    [SerializeField] private List<MonoBehaviour> attackBehaviorScripts; // IEnemyAttackBehavior를 구현하는 스크립트들의 목록

    private IEnemyPatrolBehavior patrolBehavior;
    private IEnemyChaseBehavior chaseBehavior;
    private List<IEnemyAttackBehavior> attackBehaviors; // attackBehaviorScripts에서 형변환된 공격 패턴들
    private IEnemyAttackBehavior activeAttackBehavior = null; // 지금 진행중인 공격패턴 (attackBehaviors 중 하나)

    private PlatformerMovement platformerMovement;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody2D rb;

    private enum State
    {
        Patrol,
        Chase,
        Attack,
        Stagger,
    }
    private State state = State.Patrol;

    private void Awake()
    {
        if (playerDetectionRange == null)
        {
            throw new ReferenceNotInitializedException("playerDetectionRange");
        }

        chaseBehavior = GetComponent<IEnemyChaseBehavior>();
        patrolBehavior = GetComponent<IEnemyPatrolBehavior>();

        // 할 수 있는 모든 공격을 리스트 형태로 관리.
        // 인터페이스 형식으로는 인스펙터에 표시되지 않으니
        // 그대신 IEnemyAttackBehavior를 구현하는 MonoBehavior의
        // 리스트를 받아서 여기서 형변환을 진행함.
        attackBehaviors = new List<IEnemyAttackBehavior>();
        foreach (var script in attackBehaviorScripts)
        {
            attackBehaviors.Add(script as IEnemyAttackBehavior);
        }

        platformerMovement = GetComponent<PlatformerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        playerDetectionRange.OnPlayerDetect.AddListener(HandlePlayerDetection);

        // 순찰 상태로 시작
        patrolBehavior.StartPatrol();
    }

    public void HandlePlayerDetection()
    {
        // 순찰 중이었다면 순찰을 멈추고 바로 추적을 시작해야 함.
        // 다만 행동 반경 제한 등으로 인해 추적이 불가능한 상황에서
        // 플레이어를 인식한 경우에는 순찰 상태를 유지함.
        if (state == State.Patrol && chaseBehavior.CanChasePlayer())
        {
            patrolBehavior.StopPatrol();

            state = State.Chase;
        }

        // 주변에서 인식 정보를 공유받아 이 함수가 호출된 경우
        // 자신의 PlayerDetectionRange에는 플레이어가 아직 감지되지 않은 상태이므로
        // 나중에 OnPlayerDetect가 한 번 더 실행될 수 있음.
        //
        // ex) 다른 층에 있는 적에게서 플레이어 인식 정보를 공유받은 뒤
        //     플레이어가 자신의 층으로 점프해 올라온 경우
        //
        // ex) 대기 상태에서 플레이어에게 백어택을 당해 경직 & 추적 상태에 돌입.
        //     Chase()에 의해 뒤를 돌아본 순간 플레이어가 PlayerDetectionRange 안에 들어온 경우.
        //
        // 하지만 인식 정보 공유는 최초 발견자만 수행해야 하므로
        // 여기서 PlayerDetectionRange의 정보 공유 옵션을 비활성화 해줘야 함.
        playerDetectionRange.IsDetectionShared = false;
    }

    private void FixedUpdate()
    {
        platformerMovement.HandleGroundContact();
        playerDetectionRange.SetDetectorDirection(spriteRenderer.flipX);
        animator.SetBool("IsMoving", Mathf.Abs(rb.velocity.x) > 0.01f);

        // Debug.Log(state);
        if (state == State.Chase)
        {
            // 추적 가능한 범위에 있다면 플레이어에게 접근
            if (chaseBehavior.CanChasePlayer())
            {
                chaseBehavior.ChasePlayer();

                // 여러가지 공격 방식 중에 지금 준비된 공격이 있는지 확인.
                // 실행 가능한 공격이 있다면 바로 Attack 상태로 전환함.
                foreach (var attackBehavior in attackBehaviors)
                {
                    if (attackBehavior.IsAttackPossible())
                    {
                        attackBehavior.StartAttack();

                        activeAttackBehavior = attackBehavior;
                        state = State.Attack;
                    }
                }
            }
            // 추적 범위를 벗어났다면 순찰 상태로 전환
            else
            {
                state = State.Patrol;
                patrolBehavior.StartPatrol();
            }
        }
        else if (state == State.Attack)
        {
            // 이번 공격이 끝날 때까지 기다렸다가 다시 추적 상태로 전환.
            if (activeAttackBehavior.IsAttackMotionFinished)
            {
                state = State.Chase;
            }
        }
    }

    // 공격 애니메이션의 마지막 프레임에 호출되는 이벤트.
    // 애니메이션이 완전히 끝났다는 것을 알려주며,
    // FSM에서는 Attack -> Chase 전환 조건으로서 작동한다.
    public void StopActiveAttack()
    {
        activeAttackBehavior.StopAttack();
    }
}
