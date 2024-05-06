using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class EnemyManager : MonoBehaviour {
    //Components

    private EnemyStat enemyStat;
    private Rigidbody2D rb;

    private bool moveRight = true;
    private Vector3 initialPosition;

    private void Awake()
    {
        enemyStat = GetComponent<EnemyStat>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        initialPosition = transform.position;
        enemyStat.HP.OnValueChange.AddListener(HandleHPChange);
    }

    private void HandleHPChange(float hp)
    {
        //체력을 보여줄 방법을 찾지 못해 로그로 표현해봤습니다.
        Debug.Log("아야! 적 현재 체력: " + hp);

        if (hp <= 0) { //적 사망
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player 무기인지 확인
        if (other.gameObject.layer == LayerMask.NameToLayer("Weapon"))
        {
            PlayerStat playerStat = other.GetComponentInParent<PlayerStat>();
            if (playerStat != null)
            {
                enemyStat.HP.ModifyBaseValue(-playerStat.AttackDamage.CurrentValue);
            }
        }
    }

    private void FixedUpdate() {
        if (Mathf.Abs(transform.position.x - initialPosition.x) >= enemyStat.MoveRange.CurrentValue) { //방향전환
            moveRight = !moveRight;
        }
        if (moveRight) { //우측 이동
            rb.velocity = new Vector2(enemyStat.Speed.CurrentValue, rb.velocity.y);
        }
        else { //좌측 이동
            rb.velocity = new Vector2(-enemyStat.Speed.CurrentValue, rb.velocity.y);
        }
    }
}
