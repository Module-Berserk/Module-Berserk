using System;
using System.Runtime.Serialization;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

// 버프 또는 디버프가 가능한 유동적인 스탯을 관리하는 클래스.
// 값이 바뀔 때마다 OnValueChange 이벤트로 알려줌.
[Serializable]
public class CharacterStat : ISerializable
{
    private float baseValue; // 버프와 최대치를 적용하지 않은 현재 값
    private float additiveModifier = 0f; // 합연산 버프/디버프
    private float multiplicativeModifier = 1f; // 곱연산 버프/디버프

    private CancellationTokenSource cancellationTokenSource = new(); // 비동기 버프/디버프 제거용

    public float CurrentValue {get => Mathf.Clamp((baseValue + additiveModifier) * multiplicativeModifier, MinValue, MaxValue);} // 버프와 최대치를 적용한 최종 값
    public float MinValue {get; private set;} //최솟값
    public float MaxValue {get; private set;} // 최댓값

    // 실질 수치가 변화할 때 호출되는 이벤트 (옵저버 패턴)
    // 현재 수치는 CurrentValue로 알 수 있으니 변화량을 파라미터로 알려줌.
    //
    // ex) 체력 변화량의 부호에 따라 피격 이펙트 또는 체력 회복 이펙트 재생
    public UnityEvent<float> OnValueChange {get; private set;}

    public CharacterStat(float baseValue, float minValue = float.NegativeInfinity, float maxValue = float.PositiveInfinity)
    {
        this.baseValue = baseValue;
        MinValue = minValue;
        MaxValue = maxValue;

        OnValueChange = new UnityEvent<float>();
    }

    protected CharacterStat(SerializationInfo info, StreamingContext context)
    {
        baseValue = info.GetSingle("baseValue");
        MinValue = info.GetSingle("minValue");
        MaxValue = info.GetSingle("maxValue");
        
        OnValueChange = new UnityEvent<float>();
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("baseValue", baseValue);
        info.AddValue("minValue", MinValue);
        info.AddValue("maxValue", MaxValue);
    }

    public void ResetToMaxValue()
    {
        float difference = MaxValue - CurrentValue;
        baseValue = MaxValue;

        OnValueChange.Invoke(difference);
    }

    public void ModifyBaseValue(float modifier)
    {
        float prevValue = CurrentValue;
        baseValue += modifier;
        float difference = CurrentValue - prevValue;

        OnValueChange.Invoke(difference);
    }

    public void ApplyAdditiveModifier(float modifier)
    {
        float prevValue = CurrentValue;
        additiveModifier += modifier;
        float difference = CurrentValue - prevValue;

        OnValueChange.Invoke(difference);
    }

    
    public async UniTask ApplyAdditiveModifierForDurationAsync(float modifier, float duration)
    {
        ApplyAdditiveModifier(modifier);

        await UniTask.WaitForSeconds(duration, cancellationToken: cancellationTokenSource.Token);

        ApplyAdditiveModifier(-modifier);
    }

    public void ApplyMultiplicativeModifier(float modifier)
    {
        float prevValue = CurrentValue;
        multiplicativeModifier *= modifier;
        float difference = CurrentValue - prevValue;

        OnValueChange.Invoke(difference);
    }

    
    public async UniTask ApplyMultiplicativeModifierForDurationAsync(float modifier, float duration)
    {
        // division by zero 방지
        Assert.IsFalse(Mathf.Approximately(modifier, 0f));

        ApplyMultiplicativeModifier(modifier);

        await UniTask.WaitForSeconds(duration, cancellationToken: cancellationTokenSource.Token);

        ApplyMultiplicativeModifier(1f / modifier);
    }

    // 각종 버프/디버프 모두 제거
    public void ResetModifiers()
    {
        // 비동기 버프/디버프 영향 제거
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        cancellationTokenSource = new();

        additiveModifier = 0f;
        multiplicativeModifier = 1f;
    }
}
