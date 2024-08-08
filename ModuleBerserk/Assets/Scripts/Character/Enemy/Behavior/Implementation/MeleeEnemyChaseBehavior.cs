using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlatformerMovement))]
[RequireComponent(typeof(StatRandomizer))]
public class MeleeEnemyChaseBehavior : MonoBehaviour, IEnemyChaseBehavior
{
    // Chase 상태에서 이동을 멈추기 위한 플레이어와의 거리 조건.
    // 거리가 min과 max 사이에 있는 경우에만 추적을 시도한다.
    [SerializeField] private float chaseMinDistance = 0.5f;
    [SerializeField] private float chaseMaxDistance = 5f;
    [SerializeField] private float chaseSpeed = 1f;

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
        chaseMinDistance *= randomizer.SampleRandomizationFactor();
        chaseSpeed *= randomizer.SampleRandomizationFactor();
    }

    bool IEnemyChaseBehavior.CanChasePlayer()
    {
        // 플레이어어가 존재하지 않는 경우
        if (!player)
        {
            // Debug.Log("플레이어가 없어서 추적 실패");
            return false;
        }

        // 플레이어가 있는 방향이 낭떠러지인 경우
        Vector2 chaseDirection = player.transform.position - transform.position;
        if (platformerMovement.IsOnBrink(chaseDirection.x))
        {
            // Debug.Log("플레이어가 낭떠러지 방향에 있어서 추적 실패");
            return false;
        }

        // 플레이어가 추적 가능 범위를 벗어난 경우
        if (chaseDirection.magnitude > chaseMaxDistance)
        {
            // Debug.Log("플레이어가 너무 멀어서 추적 실패");
            return false;
        }

        if (optionalMovementConfiner != null && !optionalMovementConfiner.IsPlayerInRange())
        {
            // Debug.Log("플레이어가 활동 범위 밖에 있어서 추적 실패");
            return false;
        }

        return true;
    }

    void IEnemyChaseBehavior.ChasePlayer()
    {
        Assert.IsNotNull(player);

        // 플레이어와의 x축 좌표 차이
        float displacement = player.transform.position.x - transform.position.x;

        // 플레이어 방향으로 스프라이트 설정
        spriteRenderer.flipX = displacement < 0f;

        // 아직 멈춰도 될만큼 가깝지 않다면 계속 이동
        if (Mathf.Abs(displacement) > chaseMinDistance)
        {
            float desiredSpeed = Mathf.Sign(displacement) * chaseSpeed;
            platformerMovement.UpdateMoveVelocity(desiredSpeed);
            platformerMovement.UpdateFriction(desiredSpeed);
        }
        else
        {
            platformerMovement.UpdateMoveVelocity(0f);
            platformerMovement.ApplyHighFriction();
        }
    }
}
