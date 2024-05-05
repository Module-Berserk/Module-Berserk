using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStat : CharacterStat {
    [Header("Stat")] //스탯 초기 변수
    [SerializeField] private float HP;
    [SerializeField] private float attackDamage;
    [SerializeField] private float speed;
    [SerializeField] private float moveRange;

    private void Start(){
        //초기에 Stat Dictionary에 추가함
        SetBaseStat("HP", HP);
        SetBaseStat("Attack", attackDamage);
        SetBaseStat("Speed", speed);
        SetBaseStat("MoveRange", moveRange);
    }
}
