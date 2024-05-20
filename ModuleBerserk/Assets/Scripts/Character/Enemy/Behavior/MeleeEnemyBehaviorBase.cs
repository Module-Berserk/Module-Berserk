using System.Collections;
using System.Collections.Generic;
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
//
// Note:
// 지금으로서는 대부분의 근거리 적이 거의 같은 행동을 보일 것으로 예상되므로
// 적 종류별로 생기는 소소한 차이점은 이 클래스를 상속받아 IEnemyBehavior의
// 함수 일부를 override한 EnemyBehavior를 사용하는 식으로 구현할 계획임.
// 만약 차이점이 너무 크다면 그냥 IMeleeEnemyBehavior를 새로 구현하면 됨.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class MeleeEnemyBehaviorBase : MonoBehaviour, IMeleeEnemyBehavior
{
    [Header("Chase")]
    // Chase 상태에서 이동을 멈추기 위한 플레이어와의 거리 조건
    [SerializeField] private float chaseStopDistance = 1f;
    [SerializeField] private float chaseSpeed = 1f;

    
    [Header("Melee Attack")]
    // 다음 공격까지 기다려야 하는 시간
    [SerializeField] private float meleeAttackCooltime = 3f;

    // 컴포넌트 레퍼런스
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    // 플레이어 추적용 레퍼런스
    private GameObject player;

    // 현재 대기 애니메이션이 반복 재생된 횟수
    private int idleAnimationRepetitionCount = 0;
    // 다른 대기 애니메이션으로 전환되기 위한 반복 재생 횟수
    private const int IDLE_ANIMATION_CHANGE_THRESHOLD = 6;

    // 근접 공격 쿨타임
    private float timeSinceLastMeleeAttack = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        timeSinceLastMeleeAttack += Time.deltaTime;
    }

    void IEnemyBehavior.Chase()
    {
        // 플레이어와의 x축 좌표 차이
        float displacement = player.transform.position.x - transform.position.x;

        // 플레이어 방향으로 스프라이트 설정
        spriteRenderer.flipX = displacement < 0f;

        // 아직 멈춰도 될만큼 가깝지 않다면 계속 이동
        if (Mathf.Abs(displacement) > chaseStopDistance)
        {
            animator.SetBool("IsMoving", true);
            rb.velocity = new Vector2(Mathf.Sign(displacement) * chaseSpeed, rb.velocity.y);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }
    }

    bool IEnemyBehavior.CanChasePlayer()
    {
        // 플레이어어가 존재하지 않는 경우
        if (!player)
        {
            return false;
        }

        // TODO: 플레이어가 자신이 위치한 플랫폼 범위 안에 존재하는 경우에만 true 반환
        return true;
    }

    bool IEnemyBehavior.ReturnToInitialPosition()
    {
        // TODO:
        // 1. 아직 초기 위치에 도달하지 못했다면 계속 이동하고 false 반환
        // 2. 초기 위치에 도달했다면 true 반환
        return true;
    }

    void IMeleeEnemyBehavior.MeleeAttack()
    {
        // 공격 모션 시작
        //
        // TODO:
        // 적 애니메이션 완성되면 animation controller 제대로 만들기...
        // 지금은 플레이어 애니메이션 복사해서 사용하는지라
        // 호출해야될 이벤트가 없다는 등의 에라 로그가 출력됨.
        // 모션이 잘 나오는지만 확인하기 위해 만든 임시 에셋이니 일단 무시합시다
        animator.SetTrigger("MeleeAttack");

        // 쿨타임 시작
        timeSinceLastMeleeAttack = 0f;
    }

    bool IMeleeEnemyBehavior.IsMeleeAttackReady()
    {
        return timeSinceLastMeleeAttack > meleeAttackCooltime;
    }

    void IEnemyBehavior.StartIdle()
    {
        // 대기 상태로 전환
        animator.SetBool("IsMoving", false);
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
