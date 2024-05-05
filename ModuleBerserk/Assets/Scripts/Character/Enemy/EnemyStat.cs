using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStat : MonoBehaviour
{
    [Header("HP")] //체력 변수
    [SerializeField] private float maxHP;

    [Header("Attack")] //공격력 변수
    [SerializeField] private float attackDamage;

    [Header("Speed")] //속도 변수
    [SerializeField] private float speed;

    [Header("MoveRange")] //이동반경
    [SerializeField] private float moveRange;

    public CharacterStat HP;
    public CharacterStat AttackDamage;
    public CharacterStat Speed;

    private void Awake()
    {
        HP = new CharacterStat(maxHP, 0f, maxHP);
        AttackDamage = new CharacterStat(attackDamage, 0f);
        Speed = new CharacterStat(speed, 0f);
    }
}
