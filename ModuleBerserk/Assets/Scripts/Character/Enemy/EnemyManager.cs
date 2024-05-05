using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class EnemyManager : MonoBehaviour
{
    private EnemyStat enemyStat;

    private void Awake()
    {
        enemyStat = GetComponent<EnemyStat>();
    }

    private void Start()
    {
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
}
