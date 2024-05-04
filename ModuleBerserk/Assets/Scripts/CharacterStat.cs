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
        this.modifiedValue = baseValue;
        this.maxValue = maxValue;
    }
}

public class CharacterStat : MonoBehaviour
{
    private Dictionary<string, Stat> stats = new Dictionary<string, Stat>(); //Stat 저장하는 Dictionary
    //근데 이거 Private 안잡고 걍 Public으로 가도 상관없을거 같은데
    //일단 Private잡고 Get/Set씀

    public void SetBaseStat(string statName, float baseValue, float maxValue)
    {
        if (!stats.ContainsKey(statName))
        {
            stats.Add(statName, new Stat(baseValue, maxValue));
        }
        else
        {
            stats[statName] = new Stat(baseValue, maxValue);
        }
    }

    public void ModifyStat(string statName, float modifier)
    {
        if (stats.ContainsKey(statName))
        {
            stats[statName].modifiedValue += modifier;
            stats[statName].modifiedValue = Math.Max(0, Math.Min(stats[statName].modifiedValue, stats[statName].maxValue));
        }
        else
        {
            Debug.LogError(statName + "가 없다!");
        }
    }

    public float GetModifiedStat(string statName)
    {
        if (stats.ContainsKey(statName))
        {
            return stats[statName].modifiedValue;
        }
        else
        {
            Debug.LogError(statName + "가 없다!");
            return 0f;
        }
    }
}
