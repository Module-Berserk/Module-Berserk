using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 버프 또는 디버프가 가능한 유동적인 스탯을 관리하는 클래스.
// 값이 바뀔 때마다 OnValueChange 이벤트로 알려줌.
public class CharacterStat
{
    private float baseValue; // 버프와 최대치를 적용하지 않은 현재 값
    private float additiveModifier = 0f; // 합연산 버프/디버프
    private float multiplicativeModifier = 1f; // 곱연산 버프/디버프

    public float CurrentValue {get => Mathf.Clamp((baseValue + additiveModifier) * multiplicativeModifier, MinValue, MaxValue);} // 버프와 최대치를 적용한 최종 값
    public float MinValue {get; private set;} //최솟값
    public float MaxValue {get; private set;} // 최댓값

    public UnityEvent<float> OnValueChange = new(); // 실질 수치가 변화할 때 호출되는 이벤트 (옵저버 패턴)

    public CharacterStat(float baseValue, float minValue = float.NegativeInfinity, float maxValue = float.PositiveInfinity)
    {
        this.baseValue = baseValue;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public void ModifyBaseValue(float modifier)
    {
        baseValue += modifier;

        OnValueChange.Invoke(CurrentValue);
    }

    public void ApplyAdditiveModifier(float modifier)
    {
        additiveModifier += modifier;

        OnValueChange.Invoke(CurrentValue);
    }

    public void ApplyMultiplicativeModifier(float modifier)
    {
        multiplicativeModifier *= modifier;

        OnValueChange.Invoke(CurrentValue);
    }
}
