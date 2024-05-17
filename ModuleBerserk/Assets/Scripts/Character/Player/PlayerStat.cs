using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStat : MonoBehaviour {
    [Header("HP")] //체력 변수
    [SerializeField] private float maxHP = 100f;

    [Header("Attack")] //공격력 변수
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackSpeed = 1f;

    [Header("Defense")] // 방어력 변수
    [SerializeField] private float defense = 10f;

    public CharacterStat HP;
    public CharacterStat AttackDamage;
    public CharacterStat AttackSpeed;
    public CharacterStat Defense;

    private void Awake()
    {
        HP = new CharacterStat(maxHP, 0f, maxHP);
        AttackDamage = new CharacterStat(attackDamage, 0f);
        AttackSpeed = new CharacterStat(attackSpeed, 0f);
        Defense = new CharacterStat(defense);
    }
}
