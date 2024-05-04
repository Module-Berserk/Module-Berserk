using System;
using System.Collections.Generic;
using UnityEngine;

public struct Stat //Stat Struct
{
    public float baseValue; // 기본값
    public float currentValue; // 현재값
    public float minValue; //최솟값
    public float maxValue; // 최댓값

    public Stat(float baseValue, float maxValue)
    {
        this.baseValue = baseValue;
        this.currentValue = baseValue;
        this.minValue = 0;
        this.maxValue = maxValue;
    }

    public void ApplyModifier(float modifier)
    {
        this.currentValue += modifier;
        this.currentValue = Math.Max(this.minValue, Math.Min(this.currentValue, this.maxValue)); //최댓값 및 최솟값을 초과하지 못하게함
    }
}

public class CharacterStat : MonoBehaviour
{
    private Dictionary<string, Stat> stats = new Dictionary<string, Stat>(); //Stat 저장하는 Dictionary
    //근데 이거 Private 안잡고 걍 Public으로 가도 상관없을거 같은데
    //일단 Private잡고 Get/Set씀

    public void SetBaseStat(string statName, float baseValue, float maxValue)
    {
        if (!stats.ContainsKey(statName)) //Stat 없을때
        {
            stats.Add(statName, new Stat(baseValue, maxValue));
        }
        else // Stat 있을때
        {
            var existingStat = stats[statName];
            existingStat.baseValue = baseValue;
            existingStat.maxValue = maxValue;
            existingStat.ApplyModifier(0); // 범위안에 넣기위해 실행
            stats[statName] = existingStat;
        }
    }

    public void ModifyStat(string statName, float modifier)
    {
        if (stats.ContainsKey(statName))
        {
            // Stat 변경
            var stat = stats[statName];
            stat.ApplyModifier(modifier);
            stats[statName] = stat;
        }
        else
        {
            Debug.LogError(statName + " 엥? 이게 나오면 안되는데?");
        }
    }

    public float GetModifiedStat(string statName)
    {
        if (stats.ContainsKey(statName))
        {
            return stats[statName].currentValue;
        }
        else
        {
            Debug.LogError(statName + "가 없다!");
            return 0f;
        }
    }
}
