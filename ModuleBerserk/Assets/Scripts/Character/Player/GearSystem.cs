using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;

// 플레이어의 기어 게이지를 관리하는 클래스.
// PlayerManager에서 다음 함수들을 적절히 호출해줘야 함:
// 1. 공격 성공 => OnAttackSuccess
// 2. 적에게 공격 당함 => OnPlayerHit
// 3. 긴급 회피 사용 => OnEmergencyDodge
// 4. 기어 단계 변동 => UpdateGearLevelBuff
public class GearSystem : MonoBehaviour
{
    // 기어 단계별 게이지 최대치.
    // 최대치에 도달한 상태로 일정 시간 유지한 뒤 공격을 해야 다음 단계로 넘어갈 수 있다.
    private static readonly float[] GEAR_GAUGE_UPPER_BOUND = {0f, 24f, 49f, 74f, 99f, 100f};
    // CurrentGearLevel의 최대치.
    // 첫 번째 단계가 0에서 시작하기 때문에 마지막 단계는 5임.
    private const int MAX_GEAR_LEVEL = 5;
    // 한 번 공격에 성공하면 얻는 게이지
    private const float GEAR_GAUGE_GAIN_PER_ATTACK_SUCCESS = 5f;
    // 한 번 적의 공격에 맞으면 잃는 게이지
    private const float GEAR_GAUGE_LOSS_PER_HIT = 10f;
    // 다음 기어 단계로 넘어가기 위해 게이지 최대치를 유지해야 하는 시간
    private const float MAX_GAUGE_TIME_REQUIRED_FOR_GAUGE_LEVEL_INCREASE = 3f;
    // 비전투 상태에서 1초마다 깎이는 게이지
    private const float NON_COMBAT_STATE_GEAR_GAUGE_LOSS_PER_SEC = 2f;

    // 기어 레벨별 버프 수치 (일단 합연산으로 처리)
    // TODO: 기획안에 따라 버프 수치 변경할 것
    private struct GearLevelBuff
    {
        public float Damage;
        public float Speed;
    }
    private static readonly GearLevelBuff[] GEAR_LEVEL_BUFF =
    {
        new GearLevelBuff{Damage = 0f, Speed = 0f},
        new GearLevelBuff{Damage = 1f, Speed = 0.1f},
        new GearLevelBuff{Damage = 2f, Speed = 0.2f},
        new GearLevelBuff{Damage = 3f, Speed = 0.3f},
        new GearLevelBuff{Damage = 4f, Speed = 0.4f},
        new GearLevelBuff{Damage = 5f, Speed = 0.5f},
    };


    // 0 ~ 100의 값을 갖는 게이지.
    // 적을 공격하면 게이지가 차고 반대로 공격당하면 줄어든다.
    public float CurrentGearGauge {get; private set;}
    // 게이지의 범위에 따라 총 6단계로 구분해 버프를 부여함.
    // note: 기어 단계는 0부터 시작해 최대 5까지 있음!
    public int CurrentGearLevel
    {
        get
        {
            for (int level = 0; level < 5; ++level)
            {
                if (CurrentGearGauge <= GEAR_GAUGE_UPPER_BOUND[level])
                {
                    return level;
                }
            }

            // 게이지가 100으로 가득 찬 경우
            return 5;
        }
    }
    // 공격 피격 등으로 기어 단계가 바뀐 경우 호출되는 이벤트.
    // 플레이어는 기어 단계에 따라 버프를 받으므로 수치 변동을 여기서 처리하면 됨.
    public UnityEvent OnGearLevelChange;

    // 현재 기어 단계의 최대치에 도달한 상태로 머무른 시간.
    // 이 시간이 MAX_GAUGE_TIME_REQUIRED_FOR_GAUGE_LEVEL_INCREASE보다 높아야 다음 단계로 넘어갈 수 있다.
    private float maxGaugeTime = 0f;
    // 매 프레임마다 시간을 누적해 공격 또는 피격 이벤트로부터 몇 초나 지났는지 기록함.
    // 공격 및 피격 이후 3초 동안은 전투 중으로 취급함.
    private float combatTimer = 0f;
    // 마지막으로 호출된 UpdateGearLevelBuff()에서 적용한 버프 수치.
    // 이전 버프를 제거하고 현재 값으로 갱신하기 위해 기록한다.
    private GearLevelBuff lastAppliedGearLevelBuff = GEAR_LEVEL_BUFF[0];



    // 임시 UI
    // TODO: 테스트 끝나면 삭제할 것
    public Text descriptionText;
    public Slider gaugeSlider;




    // 공격에 성공한 경우 호출되는 함수
    public void OnAttackSuccess()
    {
        ResetCombatTimer();

        // 공격에 성공할 때마다 현재 단계의 최대치를 넘지 않는 선에서 게이지를 증가시킴
        CurrentGearGauge = Mathf.Min(CurrentGearGauge + GEAR_GAUGE_GAIN_PER_ATTACK_SUCCESS, GEAR_GAUGE_UPPER_BOUND[CurrentGearLevel]);
    }

    // 적의 공격에 맞은 경우 호출되는 함수
    public void OnPlayerHit()
    {
        ResetCombatTimer();

        int prevGearLevel = CurrentGearLevel;

        // 피격당한 경우에는 현재 게이지 단계의 하한선을 무시하고 게이지가 감소함!
        // Note: 게이지 하한선은 비전투 상태의 게이지 감소에만 영향을 줌
        CurrentGearGauge = Mathf.Max(CurrentGearGauge - GEAR_GAUGE_LOSS_PER_HIT, 0f);

        // 만약 게이지 감소로 인해 기어 단계가 바뀐 경우 이벤트로 알려줌
        if (prevGearLevel != CurrentGearLevel)
        {
            OnGearLevelChange.Invoke();
        }
    }

    // 긴급 회피를 사용해 데미지를 무효화한 경우 호출되는 함수
    public void OnEmergencyDodge()
    {
        // 긴급 회피는 게이지를 25만큼 소모하므로
        // 현재 게이지가 적어도 25 이상은 되어야 함.
        Assert.IsTrue(CurrentGearGauge >= 25f);

        CurrentGearGauge -= 25f;

        // 게이지를 25 소모하면 확정적으로 기어 단계가 하나 낮아짐!
        OnGearLevelChange.Invoke();
    }

    private void ResetCombatTimer()
    {
        combatTimer = 0f;
    }

    private bool IsCombatOngoing()
    {
        // 공격 및 피격 시점으로부터 아직 3초가 지나지 않은 경우 전투 중으로 취급함
        return combatTimer < 3f;
    }

    // 게이지 최대치를 일정 시간 이상 유지해서
    // 다음 기어 단계로 넘어갈 준비가 되었다면 true를 반환
    public bool IsNextGearLevelReady()
    {
        // 이미 최대 단계인 경우
        if (CurrentGearLevel == MAX_GEAR_LEVEL)
        {
            return false;
        }

        // 아직 게이지를 충분히 채우지 못한 경우
        if (CurrentGearGauge < GEAR_GAUGE_UPPER_BOUND[CurrentGearLevel])
        {
            return false;
        }

        // 기어 게이지 최대치를 아직 기어 단계 상승에 필요한 시간만큼 유지하지 못한 경우
        if (maxGaugeTime < MAX_GAUGE_TIME_REQUIRED_FOR_GAUGE_LEVEL_INCREASE)
        {
            return false;
        }

        return true;
    }

    public void IncreaseGearLevel()
    {
        // 기어 단계 변동은 아직 최대 단계에 도달하지 못했고
        // 게이지가 현재 기어 단계의 최대치인 상태에서만 가능함
        Assert.IsTrue(CurrentGearLevel < MAX_GEAR_LEVEL);
        Assert.AreEqual(CurrentGearGauge, GEAR_GAUGE_UPPER_BOUND[CurrentGearLevel]);

        // 이미 게이지가 상한선에 도달했으므로 1만 올려줘도 다음 단계로 넘어갈 수 있음
        CurrentGearGauge++;

        OnGearLevelChange.Invoke();
    }

    // 기어 단계별 공격력 & 공격 속도 버프를 현재 기어 단계에 맞게 갱신함.
    // 마지막으로 호출된 UpdateGearLevelBuff()의 버프는 자동으로 제거.
    // 기어 단계가 바뀔 때마다 호출해두면 됨.
    public void UpdateGearLevelBuff(CharacterStat attackDamage, CharacterStat attackSpeed)
    {
        // 기존 버프 제거
        attackDamage.ApplyAdditiveModifier(-lastAppliedGearLevelBuff.Damage);
        attackSpeed.ApplyAdditiveModifier(-lastAppliedGearLevelBuff.Speed);
        
        // 신규 버프 부여
        lastAppliedGearLevelBuff = GEAR_LEVEL_BUFF[CurrentGearLevel];
        attackDamage.ApplyAdditiveModifier(lastAppliedGearLevelBuff.Damage);
        attackSpeed.ApplyAdditiveModifier(lastAppliedGearLevelBuff.Speed);
    }

    private void Update()
    {
        // 전투 상태로 돌입한 시점으로부터 몇 초나 지났는지 기록
        combatTimer += Time.deltaTime;

        // 비전투 상태라면 게이지 조금씩 감소
        if (!IsCombatOngoing())
        {
            float gear_gauge_lower_bound = CurrentGearLevel > 0 ? (GEAR_GAUGE_UPPER_BOUND[CurrentGearLevel - 1] + 1f) : 0f;
            CurrentGearGauge = Mathf.Max(CurrentGearGauge - NON_COMBAT_STATE_GEAR_GAUGE_LOSS_PER_SEC * Time.deltaTime, gear_gauge_lower_bound);
        }

        // 게이지 최대치에 도달한 경우 최대치를 유지한 시간을 기록
        if (CurrentGearGauge == GEAR_GAUGE_UPPER_BOUND[CurrentGearLevel])
        {
            maxGaugeTime += Time.deltaTime;
        }
        // 최대치가 아닌 경우 타이머 초기화
        else
        {
            maxGaugeTime = 0f;
        }

        // 아직 정식 UI가 없어서 수치 확인용으로 구현함
        // TODO: 테스트 끝나면 삭제할 것
        descriptionText.text = $"gauge: {CurrentGearGauge}\nlevel: {CurrentGearLevel}";
        gaugeSlider.value = CurrentGearGauge / 100f;
    }
}
