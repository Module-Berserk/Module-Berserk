using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStat : CharacterStat {
    [Header("Stat")] //Stat
    [SerializeField] private float HP;
    [SerializeField] private float attackDamage;
    [SerializeField] private float speed;

    private void Start(){
        //초기에 Stat Dictionary에 추가함
        SetBaseStat("HP", HP);
        SetBaseStat("Attack", attackDamage);
        SetBaseStat("Speed", speed);
    }
}
