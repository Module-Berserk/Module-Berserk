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
        // 설정에서 화면 흔들림을 켜둔 경우에만 재생함
        if (SettingsUI.IsScreenShakeEffectEnabled)
        {
            impulseSource.m_ImpulseDefinition.m_ImpulseDuration = duration;
            impulseSource.m_ImpulseDefinition.m_FrequencyGain = frequencyGain;
            impulseSource.GenerateImpulseAt(transform.position, Vector3.one * strength);
        }
    }
}
