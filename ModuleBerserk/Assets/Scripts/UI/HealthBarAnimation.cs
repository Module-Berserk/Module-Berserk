using UnityEngine;
using UnityEngine.UI;

// 체력이 감소할 때 다크소울처럼 하나의 슬라이더는 즉시 현재 체력으로 바뀌고
// 다른 하나의 슬라이더는 천천히 따라가는 애니메이션을 부여함
public class HealthBarAnimation : MonoBehaviour
{
    [Header("Slider UI")]
    [SerializeField] private Slider currentValueSlider; // 항상 즉시 바뀌는 슬라이더
    [SerializeField] private Slider followUpSlider; // 뒤따라가는 슬라이더
    [SerializeField, Range(0f, 1f)] private float followUpLerpRatio = 0.02f; // 뒤따라가는 속도 (1 = 즉시 변경)
    [SerializeField, Range(0f, 1f)] private float followUpThreshold = 0.01f; // 따라가는 슬라이더가 이 수치 이하로 가까워지면 값 동기화 (0.01 = 1%)

    private void Update()
    {
        followUpSlider.value = Mathf.Lerp(followUpSlider.value, currentValueSlider.value, followUpLerpRatio);
        if (Mathf.Abs(followUpSlider.value - currentValueSlider.value) < followUpThreshold)
        {
            followUpSlider.value = currentValueSlider.value;
        }
    }

    // value는 0에서 1의 값 (1 = full)
    public void UpdateCurrentValue(float value)
    {
        // Debug.Log($"slider updated: {value}");
        currentValueSlider.value = value;
    }
}
