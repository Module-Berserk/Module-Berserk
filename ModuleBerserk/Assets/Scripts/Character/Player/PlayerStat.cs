using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStat : CharacterStat {
    [Header("HP")] //체력 변수
    [SerializeField] private float currentHP;
    [SerializeField] private float maxHP;

    [Header("Attack")] //공격력 변수
    [SerializeField] private float attackDamage;

    [Header("Speed")] //속도 변수
    [SerializeField] private float speed;

    private void Start(){
        //초기에 Stat Dictionary에 추가함
        SetBaseStat("HP", currentHP, maxHP);
        SetBaseStat("Attack", attackDamage, attackDamage);
        SetBaseStat("Speed", speed, speed);
    }
}
