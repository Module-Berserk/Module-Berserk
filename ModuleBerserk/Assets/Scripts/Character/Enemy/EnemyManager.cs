using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour {
    private EnemyStat enemyStat;

    private void Awake() {
        enemyStat = GetComponent<EnemyStat>();
    }

    private void OnTriggerEnter2D(Collider2D other) {
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
