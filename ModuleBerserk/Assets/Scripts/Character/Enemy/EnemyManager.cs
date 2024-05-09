using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour, IDestructible {
    //Components

    private EnemyStat enemyStat;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private FlashEffectOnHit flashEffectOnHit;

    private bool moveRight = true;
    private Vector3 initialPosition;

    private void Awake()
    {
        enemyStat = GetComponent<EnemyStat>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();
    }

    private void Start() {
        initialPosition = transform.position;
        enemyStat.HP.OnValueChange.AddListener(HandleHPChange);

        // 데미지 입히는 컴포넌트 초기화
        // TODO: 데미지 입히는 테스트 끝나면 삭제할 것!
        var damager = GetComponentInChildren<ApplyDamageOnContact>();
        damager.DamageSource = Team.Enemy;
        damager.RawDamage = enemyStat.AttackDamage.CurrentValue;
    }

    private void HandleHPChange(float hpChange)
    {
        if (hpChange < 0f)
        {
            //체력을 보여줄 방법을 찾지 못해 로그로 표현해봤습니다.
            Debug.Log("아야! 적 현재 체력: " + enemyStat.HP.CurrentValue);

            _ = flashEffectOnHit.StartEffectAsync();
        }
    }

    private void FixedUpdate() {
        if (Mathf.Abs(transform.position.x - initialPosition.x) >= enemyStat.MoveRange.CurrentValue) { //방향전환
            moveRight = !moveRight;
        }
        if (moveRight) { //우측 이동
            rb.velocity = new Vector2(enemyStat.Speed.CurrentValue, rb.velocity.y);
            spriteRenderer.flipX = false;
        }
        else { //좌측 이동
            rb.velocity = new Vector2(-enemyStat.Speed.CurrentValue, rb.velocity.y);
            spriteRenderer.flipX = true;
        }
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

    void IDestructible.OnDestruction()
    {
        Destroy(gameObject);
    }
}
