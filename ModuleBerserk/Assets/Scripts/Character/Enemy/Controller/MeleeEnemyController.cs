using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

// 근접 공격을 하는 잡몹의 행동 패턴을 정의하는 클래스.
// IMeleeEnemyBehavior 인터페이스를 구현하는 스크립트를 요구한다.
//
// 만약 적절한 Behavior 스크립트가 없는 상태에서 이 스크립트를
// 게임 오브젝트에 추가하는 경우 아래와 같은 경고 창이 표시된다:
//
//   Can't add script behaviour 'IMeleeEnemyBehavior'.
//   The script class can't be abstract!
//
[RequireComponent(typeof(IMeleeEnemyBehavior))]
[RequireComponent(typeof(FlashEffectOnHit))]
public class MeleeEnemyController : MonoBehaviour, IPlayerDetector, IDestructible
{
    // 컴포넌트 레퍼런스
    private IMeleeEnemyBehavior meleeEnemyBehavior;
    private FlashEffectOnHit flashEffectOnHit;

    // IDestructible이 요구하는 스탯
    private CharacterStat hp = new(100f, 0f, 100f);
    private CharacterStat defense = new(10f, 0f);

    private enum State
    {
        Idle,
        Chase,
        Attack,
        Stagger,
    }
    private State state = State.Idle;

    private void Awake()
    {
        meleeEnemyBehavior = GetComponent<IMeleeEnemyBehavior>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
    }

    private void FixedUpdate()
    {
        if (state == State.Chase)
        {
            // 추적 가능한 범위에 있다면 플레이어에게 접근
            if (meleeEnemyBehavior.CanChasePlayer())
            {
                meleeEnemyBehavior.Chase();

                // TODO: 만약 플레이어가 공격 범위에 0.5초 이상 머무른다면 공격 상태로 전환
            }
            // 추적 범위를 벗어났다면 초기 위치로 돌아온 뒤 대기 상태로 전환
            else
            {
                bool isReturnComplete = meleeEnemyBehavior.ReturnToInitialPosition();
                if (isReturnComplete)
                {
                    state = State.Idle;
                    meleeEnemyBehavior.StartIdle();
                }
            }
        }
        else if (state == State.Attack)
        {
            // TODO:
            // 1. 공격 쿨타임이 돌아올 때마다 meleeEnemyBehavior.MeleeAttack() 호출
            // 2. 플레이어가 공격 범위를 벗어나면 Chase 상태로 전환
        }
    }

    float IPlayerDetector.GetDetectionSharingRadius()
    {
        // TODO: 정식 수치로 변경하기
        return 2f;
    }

    Vector2 IPlayerDetector.GetPosition()
    {
        return transform.position;
    }

    void IPlayerDetector.HandlePlayerDetection()
    {
        // TODO: 로그 출력 삭제하고 인식 모션 시작
        Debug.Log("플레이어 인식!");

        state = State.Chase;
    }

    CharacterStat IDestructible.GetHPStat()
    {
        return hp;
    }

    CharacterStat IDestructible.GetDefenseStat()
    {
        return defense;
    }

    Team IDestructible.GetTeam()
    {
        return Team.Enemy;
    }

    void IDestructible.OnDamage(float finalDamage, StaggerInfo staggerInfo)
    {
        // TODO:
        // 1. 경직 구현 (경직 끝나면 chase 상태로 전환)
        // 2. 아직 플레이어를 인식하지 못한 상태였다면 아래 라인 실행 (인식 & 주변에 인식 정보 공유)
        //    (this as IPlayerDetector).ShareDetectionInfo();
        flashEffectOnHit.StartEffectAsync().Forget();
    }

    void IDestructible.OnDestruction()
    {
        Destroy(gameObject);
    }
}
