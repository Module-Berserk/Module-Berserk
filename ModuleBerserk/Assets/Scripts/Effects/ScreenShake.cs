using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class ScreenShake : MonoBehaviour
{
    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void ApplyScreenShake(float strength, float duration, float frequencyGain = 1f)
    {
        impulseSource.m_ImpulseDefinition.m_ImpulseDuration = duration;
        impulseSource.m_ImpulseDefinition.m_FrequencyGain = frequencyGain;
        impulseSource.GenerateImpulse(strength);
    }
}
