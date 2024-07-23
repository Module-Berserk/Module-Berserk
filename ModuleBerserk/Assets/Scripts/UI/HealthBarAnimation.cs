using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// 체력이 감소할 때 다크소울처럼 하나의 슬라이더는 즉시 현재 체력으로 바뀌고
// 다른 하나의 슬라이더는 천천히 따라가는 애니메이션을 부여함
public class HealthBarAnimation : MonoBehaviour
{
    [Header("Slider UI")]
    [SerializeField] private Slider currentValueSlider; // 항상 즉시 바뀌는 슬라이더
    [SerializeField] private Slider followUpSlider; // 뒤따라가는 슬라이더
    [SerializeField] private FlashEffectOnHit healthRegenEffect; // 체력 찰 때 흰색으로 반짝이는 이펙트
    [SerializeField, Range(0f, 1f)] private float followUpLerpRatio = 0.02f; // 뒤따라가는 속도 (1 = 즉시 변경)
    [SerializeField, Range(0f, 1f)] private float followUpDelay = 0.5f; // 뒤따라가기 시작하는 시간
    [SerializeField, Range(0f, 1f)] private float followUpThreshold = 0.01f; // 따라가는 슬라이더가 이 수치 이하로 가까워지면 값 동기화 (0.01 = 1%)

    private float followUpTarget = 1f; // followUpSlider가 목표로 움직이는 값
    private float timeSinceLastDecrease = 0f;

    private void Update()
    {
        timeSinceLastDecrease += Time.deltaTime;
        if (timeSinceLastDecrease > followUpDelay)
        {
            followUpTarget = currentValueSlider.value;
        }

        UpdateSliderSmoothly(followUpSlider, followUpTarget);
    }

    private void UpdateSliderSmoothly(Slider slider, float targetValue)
    {
        slider.value = Mathf.Lerp(slider.value, targetValue, followUpLerpRatio);
        if (Mathf.Abs(slider.value - targetValue) < followUpThreshold)
        {
            slider.value = targetValue;
        }
    }

    // value는 0에서 1의 값 (1 = full)
    public void UpdateCurrentValue(float newValue)
    {
        // 연달아 체력이 감소하는 경우는 follow를 즉시 시작하지 않고
        // 한번에 쭉 감소하는 모습을 보여주도록 기다림.
        if (newValue < currentValueSlider.value)
        {
            timeSinceLastDecrease = 0f;
        }
        // 반대로 체력이 차는 경우는 반짝이는 이펙트 재생
        else
        {
            healthRegenEffect.StartEffectAsync().Forget();

            // follow 슬라이더는 current 슬라이더보다 늦게 감소해야하므로
            // 체력이 회복되는 상황에서는 follow의 값도 같이 변동.
            followUpTarget = Mathf.Max(followUpTarget, newValue);
        }

        currentValueSlider.value = newValue;

    }
}
