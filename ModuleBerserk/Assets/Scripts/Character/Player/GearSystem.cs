using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;

// 플레이어의 기어 게이지를 관리하는 클래스.
// PlayerManager에서 다음 함수들을 적절히 호출해줘야 함:
// 1. 공격 성공 => OnAttackSuccess
// 2. 적에게 공격 당함 => OnPlayerHit
// 3. 긴급 회피 사용 => OnEmergencyEvade
// 4. 기어 단계 변동 => UpdateGearLevelBuff
public class GearSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform gaugeNeedle;
    [SerializeField] private Image gearLevelImage;
    [SerializeField] private List<Sprite> gearLevelNumbers;


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
    private const float MAX_GAUGE_TIME_REQUIRED_FOR_GAUGE_LEVEL_INCREASE = 2f;
    // 미션 시작 시 기어가 0단계부터 1단계까지 쭉 상승하는 연출에 들일 시간.
    // 이 시간동안 게이지가 최대치로 차오르고
    // MAX_GAUGE_TIME_REQUIRED_FOR_GAUGE_LEVEL_INCREASE만큼 대기한 다음
    // 기어 단계를 1로 올리며 미션이 시작된다.
    private const float INITIAL_GUAGE_RAMPUP_TIME = 1f;
    // 비전투 상태에서 1초마다 깎이는 게이지
    private const float NON_COMBAT_STATE_GEAR_GAUGE_LOSS_PER_SEC = 2f;
    // 최대 기어 단계에 도달했을 때 게이지 수치 하락을 막는 기간
    private const float MAX_GEAR_LEVEL_PROTECTION_TIME = 5f;
    // 비전투 상태에서 게이지 하한선에 도달했을 때 기어 단계 하락을 막는 기간
    private const float NON_COMBAT_GEAR_LEVEL_PROTECTION_TIME = 3f;
    // 공격 또는 피격 이후로 이 시간 동안은 전투 상태로 판단해 기어 게이지 자연 감소가 일어나지 않음
    private const float COMBAT_DURATION = 10f;

    // 기어 레벨별 버프 수치.
    // 버프가 속도 이외의 스탯도 바꿀 가능성을 염두해서 구조체로 처리함.
    private struct GearLevelBuff
    {
        public float Speed; // 공격 속도와 이동 속도 곱연산 버프 (수치 동일함!)
    }
    private static readonly GearLevelBuff[] GEAR_LEVEL_BUFF =
    {
        new GearLevelBuff{Speed = 1f},
        new GearLevelBuff{Speed = 1.05f},
        new GearLevelBuff{Speed = 1.1f},
        new GearLevelBuff{Speed = 1.15f},
        new GearLevelBuff{Speed = 1.2f},
        new GearLevelBuff{Speed = 1.25f},
    };

    public GearSystemState CurrentState {get; private set;}


    // 각 기어 단계마다 0 ~ MAX_GEAR_GAUGE의 값을 갖는 게이지.
    // 적을 공격하면 게이지가 차고 반대로 공격당하면 줄어든다.
    // public float CurrentGearGauge {get; private set;}
    // 게이지의 범위에 따라 총 6단계로 구분해 버프를 부여함.
    // 기어 단계는 0부터 시작해 최대 5까지 있으며,
    // 0단계는 맵 입장할 때 0에서 1단계로 슉 올라가는 모습을 보여주기 위한
    // 용도이므로 실질적으로는 1단계가 최소 기어 단계임!
    // public int CurrentGearLevel {get; private set;}



    // 공격 피격 등으로 기어 단계가 바뀐 경우 호출되는 이벤트.
    // 플레이어는 기어 단계에 따라 버프를 받으므로 수치 변동을 여기서 처리하면 됨.
    public UnityEvent OnGearLevelChange {get; private set;}

    // 현재 기어 단계의 최대치에 도달한 상태로 머무른 시간.
    // 이 시간이 MAX_GAUGE_TIME_REQUIRED_FOR_GAUGE_LEVEL_INCREASE보다 높아야 다음 단계로 넘어갈 수 있다.
    private float maxGaugeTime = 0f;
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
    private GearLevelBuff lastAppliedGearLevelBuff;

    private void Awake()
    {
        OnGearLevelChange = new UnityEvent();
        OnGearLevelChange.AddListener(UpdateGearLevelImage);
    }

    private void UpdateGearLevelImage()
    {
        if (gearLevelImage != null)
        {
            gearLevelImage.sprite = gearLevelNumbers[CurrentState.GearLevel];
        }
    }

    // scene 로딩이 끝난 뒤 PlayerManager에 의해 호출되는 함수.
    // 직전 scene에서의 상태를 복원한다.
    // 
    // 주의사항:
    // OnGearLevelChange에 버프 적용 콜백을 등록한 뒤에
    // 호출해줘야만 버프 상태가 정상적으로 복원된다!!!
    public void InitializeState(GearSystemState state)
    {
        CurrentState = state;

        // 세이브 데이터를 불러올 때 스탯 버프는 모두 초기화되므로
        // "이전에 적용된 버프"는 아무 변화도 없는 0단계 기준으로 기록해놓아야 함.
        lastAppliedGearLevelBuff = GEAR_LEVEL_BUFF[0];

        // 현재 기어 단계에 맞는 버프를 다시 부여하며
        // UI 상태도 같이 복원 (Awake에서 등록된 콜백 있음)
        OnGearLevelChange.Invoke();

        if (CurrentState.GearLevel == 0)
        {
            InitialRampUpAnimationAsync().Forget();
        }
    }

    // 새로운 미션을 시작할 때 게이지가 차오르는 연출.
    // 0단계에서 실질적인 시작 상태인 1단계까지 게이지가 쭉 올라가는 모습을 보여준다.
    private async UniTask InitialRampUpAnimationAsync()
    {
        DOTween.To(() => CurrentState.GearGauge, (value) => CurrentState.GearGauge = value, MAX_GEAR_GAUGE, INITIAL_GUAGE_RAMPUP_TIME);
        await UniTask.WaitForSeconds(INITIAL_GUAGE_RAMPUP_TIME);

        // 혹시 모르니 확실하게 최대치로 설정
        CurrentState.GearGauge = MAX_GEAR_GAUGE;

        // 다음 기어 단계로 올릴 수 있을 때까지 대기
        await UniTask.WaitForSeconds(MAX_GAUGE_TIME_REQUIRED_FOR_GAUGE_LEVEL_INCREASE * 1.5f);

        // 플레이어가 수동으로 기어를 올렸을 수도 있으니
        // 중복으로 처리하지 않게 여전히 0단계인지 확인
        if (CurrentState.GearLevel == 0)
        {
            IncreaseGearLevel();
        }
    }

    // 공격에 성공한 경우 호출되는 함수
    public void OnAttackSuccess()
    {
        ResetCombatTimer();

        // 공격에 성공할 때마다 현재 단계의 최대치를 넘지 않는 선에서 게이지를 증가시킴
        float gaugeGain = GEAR_GAUGE_GAIN_PER_ATTACK_SUCCESS * CurrentState.GearGaugeGainCoefficient.CurrentValue;
        CurrentState.GearGauge = Mathf.Min(CurrentState.GearGauge + gaugeGain, MAX_GEAR_GAUGE);
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
        if (CurrentState.GearGauge < GEAR_GAUGE_LOSS_PER_HIT && CurrentState.GearLevel > 1)
        {
            CurrentState.GearLevel--;
            CurrentState.GearGauge += MAX_GEAR_GAUGE;

            OnGearLevelChange.Invoke();
        }

        // Note: 최소 기어 단계였다면 게이지가 음수가 되어버릴 수 있으므로 최소 0 유지
        CurrentState.GearGauge = Mathf.Max(0f, CurrentState.GearGauge - GEAR_GAUGE_LOSS_PER_HIT);
    }

    // 긴급 회피는 기어 단계를 하락시키므로 최소 1단계
    public bool IsEmergencyEvadePossible()
    {
        return CurrentState.GearLevel > 1;
    }

    // 긴급 회피를 사용하는 경우 호출되는 함수.
    // 일반 회피와 다르게 데미지 무효화가 가능한 대신 기어 단계를 하나 떨어트린다.
    public void OnEmergencyEvade()
    {
        Assert.IsTrue(CurrentState.GearLevel > 1);

        CurrentState.GearLevel--;
        OnGearLevelChange.Invoke();
    }

    private void ResetCombatTimer()
    {
        combatTimer = 0f;
    }

    private bool IsCombatOngoing()
    {
        // 공격 및 피격 시점으로부터 일정 시간이 않은 경우 전투 중으로 취급함
        return combatTimer < COMBAT_DURATION;
    }

    // 게이지 최대치를 일정 시간 이상 유지해서
    // 다음 기어 단계로 넘어갈 준비가 되었다면 true를 반환
    public bool IsNextGearLevelReady()
    {
        // 이미 최대 단계인 경우
        if (CurrentState.GearLevel == MAX_GEAR_LEVEL)
        {
            return false;
        }

        // 아직 게이지를 충분히 채우지 못한 경우
        if (CurrentState.GearGauge < MAX_GEAR_GAUGE)
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
        Assert.IsTrue(CurrentState.GearLevel < MAX_GEAR_LEVEL);
        Assert.AreEqual(CurrentState.GearGauge, MAX_GEAR_GAUGE);

        // 단계가 올라가면 게이지를 0부터 다시 채우기 시작
        CurrentState.GearLevel++;
        CurrentState.GearGauge = 0;

        OnGearLevelChange.Invoke();

        // 기어 단계가 최대치에 도달하면 잠시동안 게이지 하락을 막음
        if (CurrentState.GearLevel == MAX_GEAR_LEVEL)
        {
            remainingGaugeProtectionTime = MAX_GEAR_LEVEL_PROTECTION_TIME;
        }
    }

    // 기어 단계별 공격력 & 공격 속도 버프를 현재 기어 단계에 맞게 갱신함.
    // 마지막으로 호출된 UpdateGearLevelBuff()의 버프는 자동으로 제거.
    // 기어 단계가 바뀔 때마다 호출해두면 됨.
    public void UpdateGearLevelBuff(CharacterStat attackSpeed, CharacterStat moveSpeed)
    {
        RemoveOldBuff(attackSpeed, moveSpeed);
        ApplyNewBuff(attackSpeed, moveSpeed);
    }

    private void ApplyNewBuff(CharacterStat attackSpeed, CharacterStat moveSpeed)
    {
        lastAppliedGearLevelBuff = GEAR_LEVEL_BUFF[CurrentState.GearLevel];
        attackSpeed.ApplyMultiplicativeModifier(lastAppliedGearLevelBuff.Speed);
        moveSpeed.ApplyMultiplicativeModifier(lastAppliedGearLevelBuff.Speed);
    }

    public void RemoveOldBuff(CharacterStat attackSpeed, CharacterStat moveSpeed)
    {
        attackSpeed.ApplyMultiplicativeModifier(1f / lastAppliedGearLevelBuff.Speed);
        moveSpeed.ApplyMultiplicativeModifier(1f / lastAppliedGearLevelBuff.Speed);
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
        if (CurrentState.GearGauge == MAX_GEAR_GAUGE)
        {
            maxGaugeTime += Time.deltaTime;
        }
        // 최대치가 아닌 경우 타이머 초기화
        else
        {
            maxGaugeTime = 0f;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        // 기어 단계 상승이 가능한 상황이라면 빨간 영역에서 랜덤하게 바늘이 흔들리고
        // 그게 아니라면 정직하게 게이지에 비례해서 회전.
        float gearGuagePercent = CurrentState.GearGauge / MAX_GEAR_GAUGE;
        if (IsNextGearLevelReady())
        {
            gearGuagePercent = Random.Range(1f, 1.15f);
        }

        float targetZAngle = Mathf.LerpUnclamped(359f, 170f, gearGuagePercent);
        float newZAngle = Mathf.Lerp(gaugeNeedle.eulerAngles.z, targetZAngle, 0.1f);
        gaugeNeedle.rotation = Quaternion.Euler(0f, 0f, newZAngle);
    }

    // 비전투 상태에서의 기어 게이지 하락 로직
    private void HandleNaturalGaugeDecrease()
    {
        // 이미 최소 기어 단계인 경우는 더 하락할 게이지조차 없음
        if (CurrentState.GearLevel == 0)
        {
            return;
        }

        // 하한선까지는 계속 감소
        CurrentState.GearGauge = Mathf.Max(0f, CurrentState.GearGauge - NON_COMBAT_STATE_GEAR_GAUGE_LOSS_PER_SEC * Time.deltaTime);

        // 하한선에 도달한 경우 잠깐의 유예 시간을 준 뒤 단계를 하나 감소시킴
        // 단, 기어 레벨이 1인 경우는 더 떨어질 레벨이 없으므로 현상태를 유지함
        if (CurrentState.GearGauge == 0f && CurrentState.GearLevel > 1)
        {
            gaugeLowerBoundDuration += Time.deltaTime;
            if (gaugeLowerBoundDuration > NON_COMBAT_GEAR_LEVEL_PROTECTION_TIME)
            {
                gaugeLowerBoundDuration = 0f;

                // 단계가 감소한 뒤에는 게이지가 이전 단계의 최대치에서 감소하기 시작함
                CurrentState.GearLevel--;
                CurrentState.GearGauge = MAX_GEAR_GAUGE;

                OnGearLevelChange.Invoke();
            }
        }
    }
}
