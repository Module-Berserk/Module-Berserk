using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 아주 단순한 근접 공격 잡몹의 행동을 정의함:
// 1. 자신이 서있는 플랫폼 위에 한해 주인공을 추적
// 2. 근접 공격 모션이 하나만 존재
// 3. 대기 모션이 두 가지 존재하며 반복 재생 횟수가
//    일정 수치를 넘어가면 둘 중에서 다른 모션으로 전환됨.
//
// Animation Controller 의존성:
// 1. 대기 모션의 마지막 프레임에 OnIdleAnimationEnd 이벤트 필요
// 2. ChangeIdleAnimation 트리거에 반응해 두 종류의 대기 모션을 번갈아가며 사용해야 함
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
    // 컴포넌트 레퍼런스
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    // 현재 대기 애니메이션이 반복 재생된 횟수
    private int idleAnimationRepetitionCount = 0;
    // 다른 대기 애니메이션으로 전환되기 위한 반복 재생 횟수
    private const int IDLE_ANIMATION_CHANGE_THRESHOLD = 6;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void IEnemyBehavior.Chase()
    {
        // TODO:
        // 1. 플랫폼 위에서 플레이어를 추적하는 로직 구현
        // 2. 이동 방향에 맞게 spriteRenderer.flipX 설정
        Debug.Log("근접 공격 잡몹: 플레이어 추적 중");
    }

    bool IEnemyBehavior.CanChasePlayer()
    {
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
        // TODO: 공격 모션 시작
        Debug.Log("근접 공격 잡몹: 근접 공격 시작");
    }

    void IEnemyBehavior.StartIdle()
    {
        // TODO: 대기 상태로 전환
        Debug.Log("근접 공격 잡몹: 대기 시작");
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
