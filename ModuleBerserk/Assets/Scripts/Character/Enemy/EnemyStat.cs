using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStat : MonoBehaviour {
    [Header("HP")] //체력 변수
    [SerializeField] private float maxHP;

    [Header("Attack")] //공격력 변수
    [SerializeField] private float attackDamage;
    [SerializeField] private float speed;
    [SerializeField] private float moveRange;

    [Header("Defense")] // 방어력 변수
    [SerializeField] private float defense;

    public CharacterStat HP;
    public CharacterStat AttackDamage;
    public CharacterStat Speed;
    public CharacterStat MoveRange;
    public CharacterStat Defense;

    private void Awake()
    {
        HP = new CharacterStat(maxHP, 0f, maxHP);
        AttackDamage = new CharacterStat(attackDamage, 0f);
        Speed = new CharacterStat(speed, 0f);
        MoveRange = new CharacterStat(moveRange, 0f);
        Defense = new CharacterStat(defense);
    }
}
