using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class EnemyManager : MonoBehaviour, IDestructible {
    //Components

    private EnemyStat enemyStat;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private FlashEffectOnHit flashEffectOnHit;
    private Vector3 initialPosition;

    private enum State
    {
        Patrol, // 좌우로 왔다갔다 반복하는 상태
        Stagger, // 공격 당해서 잠깐 경직된 상태
    }
    private State state = State.Patrol;

    private void Awake()
    {
        enemyStat = GetComponent<EnemyStat>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
    }

    private void Start()
    {
        initialPosition = transform.position;
        // TODO: enemyStat.HP.OnValueChange에 체력바 UI 업데이트 함수 등록

        // ApplyDamageOnContact 컴포넌트에서 적의 공격력 스탯을 사용하도록 설정
        var applyDamageOnContact = GetComponentInChildren<ApplyDamageOnContact>();
        applyDamageOnContact.RawDamage = enemyStat.AttackDamage;
    }

    private void FixedUpdate() {
        if (state == State.Patrol)
        {
            PerformPatrolMovement();
        }
        else
        {
            // 경직과 같이 부여된 넉백 효과 부드럽게 감소
            float updatedVelocityX = Mathf.MoveTowards(rb.velocity.x, 0f, 30f * Time.deltaTime);
            rb.velocity = new Vector2(updatedVelocityX, rb.velocity.y);
        }
    }

    private void PerformPatrolMovement()
    {
        float offsetFromInitialPosition = transform.position.x - initialPosition.x;
        float patrolRange = enemyStat.MoveRange.CurrentValue;
        
        // 왼쪽 순찰 경계를 넘어서면 오른쪽으로 방향 전환
        if (offsetFromInitialPosition < -patrolRange)
        {
            spriteRenderer.flipX = false;
        }
        // 오른쪽 순찰 경계를 넘어서면 왼쪽으로 방향 전환
        else if (offsetFromInitialPosition > patrolRange)
        {
            spriteRenderer.flipX = true;
        }

        // 바라보는 방향으로 속도 일정하게 유지
        rb.velocity = new Vector2(enemyStat.Speed.CurrentValue * (spriteRenderer.flipX ? -1f : 1f), rb.velocity.y);
    }

    CharacterStat IDestructible.GetHPStat()
    {
        return enemyStat.HP;
    }

    CharacterStat IDestructible.GetDefenseStat()
    {
        return enemyStat.Defense;
    }

    Team IDestructible.GetTeam()
    {
        return Team.Enemy;
    }

    void IDestructible.OnDamage(float finalDamage, StaggerInfo staggerInfo)
    {
        flashEffectOnHit.StartEffectAsync().Forget();

        ApplyStaggerForDurationAsync(staggerInfo.direction * 3.0f, 0.4f).Forget();
    }

    // 데미지를 입으면 잠시 경직 상태에 빠진 후 다시 해당 위치부터 순찰 시작
    private async UniTask ApplyStaggerForDurationAsync(Vector2 staggerForce, float duration)
    {
        rb.AddForce(staggerForce, ForceMode2D.Impulse);
        state = State.Stagger;

        // 공격받은 방향 바라보기 (오른쪽으로 넉백 <=> 왼쪽에서 공격당함)
        spriteRenderer.flipX = staggerForce.x > 0f;

        await UniTask.WaitForSeconds(duration);

        state = State.Patrol;
    }

    void IDestructible.OnDestruction()
    {
        Destroy(gameObject);
    }
}
