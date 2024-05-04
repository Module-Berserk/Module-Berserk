using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStat : CharacterStat {
    [Header("HP")] //체력 변수
    [SerializeField] private float HP;

    [Header("Attack")] //공격력 변수
    [SerializeField] private float attackDamage;

    [Header("Speed")] //속도 변수
    [SerializeField] private float speed;

    [Header("MoveRange")] //이동반경
    [SerializeField] private float moveRange;

    private void Start(){
        //초기에 Stat Dictionary에 추가함
        SetBaseStat("HP", HP);
        SetBaseStat("Attack", attackDamage);
        SetBaseStat("Speed", speed);
        SetBaseStat("MoveRange", moveRange);
    }
}
