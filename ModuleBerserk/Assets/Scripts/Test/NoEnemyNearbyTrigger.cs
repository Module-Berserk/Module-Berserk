using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Trigger와 조건부 상호작용을 테스트하기 위해 만든 클래스.
// 범위 내에 적이 없는 경우 활성화된다.
public class NoEnemyNearbyTrigger : Trigger
{
    private int numEnemiesWithinRange = 0;

    private void Awake()
    {
        // 범위 내에 적이 없는 상태로 시작하면 OnTriggerExit2D
        // 이벤트가 발생하지 않아 비활성화 상태로 유지됨.
        // 이를 막기 위해 활성화 상태로 시작함.
        Activate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // 적이 범위 안에 들어오면 비활성화
            if (numEnemiesWithinRange == 0)
            {
                Debug.Log("범위 안에 적 있음");
                Deactivate();
            }

            numEnemiesWithinRange++;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            numEnemiesWithinRange--;

            // 모든 적이 범위에서 나가면 활성화
            if (numEnemiesWithinRange == 0)
            {
                Debug.Log("범위 안에 적 없음");
                Activate();
            }
        }
    }
}
