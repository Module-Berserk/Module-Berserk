using DG.Tweening;
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
    // CurrentGearLevel의 최대치.
    // 첫 번째 단계가 0에서 시작하기 때문에 마지막 단계는 5임.
    private const int MAX_GEAR_LEVEL = 5;
    // 각 단계마다 가질 수 있는 게이지의 최대치.
    private const int MAX_GEAR_GAUGE = 25;
    // 한 번 공격에 성공하면 얻는 게이지
    private const float GEAR_GAUGE_GAIN_PER_ATTACK_SUCCESS = 5f;
    // 한 번 적의 공격에 맞으면 잃는 게이지
    private const float GEAR_GAUGE_LOSS_PER_HIT = 10f;
    // 다음 기어 단계로 넘어가기 위해 게이지 최대치를 유지해야 하는 시간
    private const float MAX_GAUGE_TIME_REQUIRED_FOR_GAUGE_LEVEL_INCREASE = 3f;
    // 비전투 상태에서 1초마다 깎이는 게이지
    private const float NON_COMBAT_STATE_GEAR_GAUGE_LOSS_PER_SEC = 2f;
    // 최대 기어 단계에 도달했을 때 게이지 수치 하락을 막는 기간
    private const float MAX_GEAR_LEVEL_PROTECTION_TIME = 5f;
    // 비전투 상태에서 게이지 하한선에 도달했을 때 기어 단계 하락을 막는 기간
    private const float NON_COMBAT_GEAR_LEVEL_PROTECTION_TIME = 3f;

    // 기어 레벨별 버프 수치
    private struct GearLevelBuff
    {
        public float Damage; // 공격력 합연산 버프 
        public float Speed; // 공격 속도와 이동 속도 곱연산 버프 (수치 동일함!)
    }
    private static readonly GearLevelBuff[] GEAR_LEVEL_BUFF =
    {
        new GearLevelBuff{Damage = 0f, Speed = 1f},
        new GearLevelBuff{Damage = 3f, Speed = 1.05f},
        new GearLevelBuff{Damage = 6f, Speed = 1.1f},
        new GearLevelBuff{Damage = 9f, Speed = 1.15f},
        new GearLevelBuff{Damage = 12f, Speed = 1.2f},
        new GearLevelBuff{Damage = 15f, Speed = 1.25f},
    };


    // 각 기어 단계마다 0 ~ MAX_GEAR_GAUGE의 값을 갖는 게이지.
    // 적을 공격하면 게이지가 차고 반대로 공격당하면 줄어든다.
    public float CurrentGearGauge {get; private set;}
    // 게이지의 범위에 따라 총 6단계로 구분해 버프를 부여함.
    // note: 기어 단계는 0부터 시작해 최대 5까지 있음!
    public int CurrentGearLevel {get; private set;}
    // 공격 피격 등으로 기어 단계가 바뀐 경우 호출되는 이벤트.
    // 플레이어는 기어 단계에 따라 버프를 받으므로 수치 변동을 여기서 처리하면 됨.
    public UnityEvent OnGearLevelChange;

    // 현재 기어 단계의 최대치에 도달한 상태로 머무른 시간.
    // 이 시간이 MAX_GAUGE_TIME_REQUIRED_FOR_GAUGE_LEVEL_INCREASE보다 높아야 다음 단계로 넘어갈 수 있다.
    // 0단계에서는 공격하면 바로 1단계로 넘어갈 수 있도록 아주 큰 초기값을 부여.
    private float maxGaugeTime = 10000f;
    // 최대 기어 단계 도달에 의한 게이지 하락 보호 기간.
    // 이 수치가 0 이상이면 무슨 일이 있어도 게이지가 떨어지지 않는다.
    private float remainingGaugeProtectionTime = 0f;
    // 비전투 상태에서 게이지 하한선에 머무른 시간
    private float gaugeLowerBoundDuration = 0f;
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
    public RectTransform gaugeArrow; // z축 각도: 최소 80, 최대 -137, 붉은 영역 최대 -165


    private void Start()
    {
        // TODO: 맵 입장할 때 0단계에서 시작해 1단계까지 쭉 올라가는 모습 보여주기
        CurrentGearGauge = 0;
        CurrentGearLevel = 0;
    }

    // 공격에 성공한 경우 호출되는 함수
    public void OnAttackSuccess()
    {
        ResetCombatTimer();

        // 공격에 성공할 때마다 현재 단계의 최대치를 넘지 않는 선에서 게이지를 증가시킴
        CurrentGearGauge = Mathf.Min(CurrentGearGauge + GEAR_GAUGE_GAIN_PER_ATTACK_SUCCESS, MAX_GEAR_GAUGE);
    }

    // 적의 공격에 맞은 경우 호출되는 함수
    public void OnPlayerHit()
    {
        ResetCombatTimer();

        // 최대 기어 단계 도달에 의한 게이지 하락 보호 기간인 경우 변동 x
        if (remainingGaugeProtectionTime > 0f)
        {
            return;
        }

        // 피격당한 경우에는 현재 게이지 단계의 하한선을 무시하고 게이지가 감소함!
        // Note: 게이지 하한선은 비전투 상태의 게이지 감소에만 영향을 줌
        //
        // 깎을 게이지가 없는 경우 기어가 한 단계 내려감.
        // 이 경우 뺄셈에서 받아내림을 하듯이 처리해줘야 함
        // ex) 기어 2단계 게이지 3에서 10 차감 ==> 기어 1단계 게이지 (3 + MAX_GEAR_GAUGE)에서 10 차감
        if (CurrentGearGauge < GEAR_GAUGE_LOSS_PER_HIT && CurrentGearLevel > 1)
        {
            CurrentGearLevel--;
            CurrentGearGauge += MAX_GEAR_GAUGE;

            OnGearLevelChange.Invoke();
        }

        // Note: 최소 기어 단계였다면 게이지가 음수가 되어버릴 수 있으므로 최소 0 유지
        CurrentGearGauge = Mathf.Max(0f, CurrentGearGauge - GEAR_GAUGE_LOSS_PER_HIT);
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
        if (CurrentGearGauge < MAX_GEAR_GAUGE)
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
        Assert.AreEqual(CurrentGearGauge, MAX_GEAR_GAUGE);

        // 단계가 올라가면 게이지를 0부터 다시 채우기 시작
        CurrentGearLevel++;
        CurrentGearGauge = 0;

        OnGearLevelChange.Invoke();

        // 기어 단계가 최대치에 도달하면 잠시동안 게이지 하락을 막음
        if (CurrentGearLevel == MAX_GEAR_LEVEL)
        {
            remainingGaugeProtectionTime = MAX_GEAR_LEVEL_PROTECTION_TIME;
        }
    }

    // 기어 단계별 공격력 & 공격 속도 버프를 현재 기어 단계에 맞게 갱신함.
    // 마지막으로 호출된 UpdateGearLevelBuff()의 버프는 자동으로 제거.
    // 기어 단계가 바뀔 때마다 호출해두면 됨.
    public void UpdateGearLevelBuff(CharacterStat attackDamage, CharacterStat attackSpeed, CharacterStat moveSpeed)
    {
        // 기존 버프 제거
        attackDamage.ApplyAdditiveModifier(-lastAppliedGearLevelBuff.Damage);
        attackSpeed.ApplyMultiplicativeModifier(1f / lastAppliedGearLevelBuff.Speed);
        moveSpeed.ApplyMultiplicativeModifier(1f / lastAppliedGearLevelBuff.Speed);
        
        // 신규 버프 부여
        lastAppliedGearLevelBuff = GEAR_LEVEL_BUFF[CurrentGearLevel];
        attackDamage.ApplyAdditiveModifier(lastAppliedGearLevelBuff.Damage);
        attackSpeed.ApplyMultiplicativeModifier(lastAppliedGearLevelBuff.Speed);
        moveSpeed.ApplyMultiplicativeModifier(lastAppliedGearLevelBuff.Speed);
    }

    private void Update()
    {
        // 전투 상태로 돌입한 시점으로부터 몇 초나 지났는지 기록
        combatTimer += Time.deltaTime;

        // 기어 레벨 최대치에 달성한 경우 주어지는 게이지 하락 방지 기간
        remainingGaugeProtectionTime -= Time.deltaTime;

        // 비전투 상태라면 게이지 조금씩 감소
        if (!IsCombatOngoing())
        {
            HandleNaturalGaugeDecrease();
        }

        // 게이지 최대치에 도달한 경우 최대치를 유지한 시간을 기록
        if (CurrentGearGauge == MAX_GEAR_GAUGE)
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

        float targetZAngle = Mathf.Lerp(80f, -137f, CurrentGearGauge / MAX_GEAR_GAUGE);
        gaugeArrow.rotation = Quaternion.Euler(0f, 0f, targetZAngle);
    }

    // 비전투 상태에서의 기어 게이지 하락 로직
    private void HandleNaturalGaugeDecrease()
    {
        // 이미 최소 기어 단계인 경우는 더 하락할 게이지조차 없음
        if (CurrentGearLevel == 0)
        {
            return;
        }

        // 하한선까지는 계속 감소
        CurrentGearGauge = Mathf.Max(0f, CurrentGearGauge - NON_COMBAT_STATE_GEAR_GAUGE_LOSS_PER_SEC * Time.deltaTime);

        // 하한선에 도달한 경우 잠깐의 유예 시간을 준 뒤 단계를 하나 감소시킴
        if (CurrentGearGauge == 0f)
        {
            gaugeLowerBoundDuration += Time.deltaTime;
            if (gaugeLowerBoundDuration > NON_COMBAT_GEAR_LEVEL_PROTECTION_TIME)
            {
                gaugeLowerBoundDuration = 0f;

                // 단계가 감소한 뒤에는 게이지가 이전 단계의 최대치에서 감소하기 시작함
                CurrentGearLevel--;
                CurrentGearGauge = MAX_GEAR_GAUGE;

                OnGearLevelChange.Invoke();
            }
        }
    }
}
