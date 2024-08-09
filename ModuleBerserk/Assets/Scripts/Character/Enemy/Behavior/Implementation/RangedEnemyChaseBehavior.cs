using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlatformerMovement))]
[RequireComponent(typeof(StatRandomizer))]
public class RangedEnemyChaseBehavior : MonoBehaviour, IEnemyChaseBehavior
{
    // Chase 상태에서 이동을 멈추기 위한 플레이어와의 거리 조건.
    // 너무 가까우면 도망가고 너무 멀면 접근하지만 그 중간에 있는 경우 가만히 서있는다.
    [SerializeField] private float runAwayStartDistance = 3f;
    [SerializeField] private float chaseStopMaxDistance = 5f;
    [SerializeField] private float chaseMaxDistance = 30f;
    [SerializeField] private float chaseSpeed = 1f;


    [Header("Debug")]
    [SerializeField] private bool logChaseFailureReason = false;

    private SpriteRenderer spriteRenderer;
    private PlatformerMovement platformerMovement;
    private EnemyMovementConfiner optionalMovementConfiner; // null이어도 됨!
    private GameObject player;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        platformerMovement = GetComponent<PlatformerMovement>();
        optionalMovementConfiner = GetComponent<EnemyMovementConfiner>();
        player = GameObject.FindWithTag("Player");

        RandomizeStats();
    }

    private void RandomizeStats()
    {
        var randomizer = GetComponent<StatRandomizer>();
        runAwayStartDistance *= randomizer.SampleRandomizationFactor();
        chaseStopMaxDistance *= randomizer.SampleRandomizationFactor();
        chaseSpeed *= randomizer.SampleRandomizationFactor();
    }

    bool IEnemyChaseBehavior.CanChasePlayer()
    {
        // 플레이어어가 존재하지 않는 경우
        if (!player)
        {
            if (logChaseFailureReason)
            {
                Debug.Log("플레이어가 없어서 추적 실패", gameObject);
            }
            return false;
        }

        // 움직일 방향이 낭떠러지인 경우
        if (platformerMovement.IsOnBrink(GetChaseSpeed()))
        {
            if (logChaseFailureReason)
            {
                Debug.Log($"플레이어가 낭떠러지 방향에 있어서 추적 실패\nchase speed = {GetChaseSpeed()}", gameObject);
            }
            return false;
        }

        // 플레이어가 추적 가능 범위를 벗어난 경우
        if (IsPlayerOutOfRange())
        {
            if (logChaseFailureReason)
            {
                Debug.Log("플레이어가 너무 멀어서 추적 실패", gameObject);
            }
            return false;
        }

        if (optionalMovementConfiner != null && !optionalMovementConfiner.IsPlayerInRange())
        {
            if (logChaseFailureReason)
            {
                Debug.Log("플레이어가 활동 범위 밖에 있어서 추적 실패", gameObject);
            }
            return false;
        }

        return true;
    }

    private float GetChaseSpeed()
    {
        Vector2 displacement = player.transform.position - transform.position;

        float distance = displacement.magnitude;
        if (distance < runAwayStartDistance)
        {
            return -Mathf.Sign(displacement.x) * chaseSpeed;
        }
        else if (distance >= runAwayStartDistance && distance < chaseStopMaxDistance)
        {
            return 0f;
        }
        else
        {
            return Mathf.Sign(displacement.x) * chaseSpeed;
        }
    }

    private bool IsPlayerOutOfRange()
    {
        Vector2 displacement = player.transform.position - transform.position;
        return displacement.magnitude > chaseMaxDistance;
    }

    void IEnemyChaseBehavior.ChasePlayer()
    {
        Assert.IsNotNull(player);

        float desiredSpeed = GetChaseSpeed();
        platformerMovement.UpdateMoveVelocity(desiredSpeed);
        platformerMovement.UpdateFriction(desiredSpeed);

        // 계속 움직이는 중이라면 이동 방향으로 스프라이트 설정
        if (Mathf.Abs(desiredSpeed) > 0.1f)
        {
            spriteRenderer.flipX = desiredSpeed < 0f;
        }
        // 모종의 이유로 정지했다면 플레이어를 바라봄
        else
        {
            spriteRenderer.flipX = player.transform.position.x < transform.position.x;
        }
    }
}
