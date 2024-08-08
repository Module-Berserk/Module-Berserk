using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlatformerMovement))]
[RequireComponent(typeof(StatRandomizer))]
public class DefaultEnemyPatrolBehavior : MonoBehaviour, IEnemyPatrolBehavior
{
    // 순찰 하위 상태인 '걷기' 또는 '대기'의 지속시간 범위.
    // 하위 상태가 변경될 때마다 min ~ max 사이의 랜덤한 시간이 할당된다.
    [SerializeField] private float minPatrolSubbehaviorDuration = 1f;
    [SerializeField] private float maxPatrolSubbehaviorDuration = 4f;
    // 순찰 중 걷기 상태의 이동 속도
    [SerializeField] private float patrolSpeed = 1f;

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

    private SpriteRenderer spriteRenderer;
    private PlatformerMovement platformerMovement;
    private EnemyMovementConfiner optionalMovementConfiner;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        platformerMovement = GetComponent<PlatformerMovement>();
        optionalMovementConfiner = GetComponent<EnemyMovementConfiner>();

        var randomizer = GetComponent<StatRandomizer>();
        patrolSpeed *= randomizer.SampleRandomizationFactor();
    }

    private void FixedUpdate()
    {
        if (isPatrolling)
        {
            PerformPatrol();
        }
    }

    void IEnemyPatrolBehavior.StartPatrol()
    {
        isPatrolling = true;
        samePatrolDirectionCount = 0;
        SetPatrolSubbehavior(PatrolSubbehavior.Pause);
    }

    void IEnemyPatrolBehavior.StopPatrol()
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

        // 다음과 같은 경우 반대 방향 선택:
        // 1. 같은 순찰 방향이 3회 이상 걸림
        // 2. 해당 방향이 낭떠러지
        // 3. 해당 방향으로 가면 활동 제한 범위를 벗어남 (optional)
        if (samePatrolDirectionCount >= 3 || platformerMovement.IsOnBrink(patrolSpeed) || IsMovingOutsideConfinement(patrolSpeed))
        {
            patrolSpeed *= -1f;
            samePatrolDirectionCount = 1;
        }
    }

    private bool IsMovingOutsideConfinement(float patrolSpeed)
    {
        return optionalMovementConfiner != null && optionalMovementConfiner.IsMovingOutsideRestrictedArea(patrolSpeed);
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
        spriteRenderer.flipX = patrolSpeed < 0f;

        // 해당 방향이 낭떠러지가 아니라면 이동
        if (!platformerMovement.IsOnBrink(patrolSpeed) && !IsMovingOutsideConfinement(patrolSpeed))
        {
            platformerMovement.UpdateMoveVelocity(patrolSpeed);
            platformerMovement.UpdateFriction(patrolSpeed);
        }
        // 낭떠러지를 만나면 즉시 '대기' 상태로 전환
        else
        {
            SetPatrolSubbehavior(PatrolSubbehavior.Pause);
        }
    }

    // 순찰 하위 상태 중 '대기'에 해당하는 행동
    private void PatrolPause()
    {
        platformerMovement.UpdateMoveVelocity(0f);
        platformerMovement.ApplyHighFriction();
    }
}
