using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour {
    //Components
    private EnemyStat enemyStat;
    private Rigidbody2D rb;

    private bool moveRight = true;
    private Vector3 initialPosition;

    private void Awake() {
        enemyStat = GetComponent<EnemyStat>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start() {
        initialPosition = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Weapon")) { // Player 무기인지 확인
            PlayerStat player = other.GetComponentInParent<PlayerStat>();
            if (player != null) {
                
                enemyStat.ModifyStat("HP", -player.GetModifiedStat("Attack"));
                Debug.Log("아야! 적 현재 체력: " + enemyStat.GetModifiedStat("HP")); //체력을 보여줄 방법을 찾지 못해 로그로 표현해봤습니다.

                if (enemyStat.GetModifiedStat("HP") <= 0) { //적 사망
                    Destroy(gameObject);
                }
            }
        }
    }

    private void FixedUpdate() {
        if (Mathf.Abs(transform.position.x - initialPosition.x) >= enemyStat.GetModifiedStat("MoveRange")) { //방향전환
            moveRight = !moveRight;
        }
        if (moveRight) { //우측 이동
            rb.velocity = new Vector2(enemyStat.GetModifiedStat("Speed"), rb.velocity.y);
        }
        else { //좌측 이동
            rb.velocity = new Vector2(-enemyStat.GetModifiedStat("Speed"), rb.velocity.y);
        }
    }
}
