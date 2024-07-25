using System;

[Serializable]
public class GearSystemState
{
    // 기어 게이지를 0에서 1까지 올리는 연출을 보여줄 것인지 결정.
    // 은신처에서 새로운 미션을 시작할 때에만 true로 설정해야 한다.
    public bool NeedInitialRampUp;
    public float GearGauge;
    public int GearLevel;
    public CharacterStat GearGaugeGainCoefficient;

    public static GearSystemState CreateDummyState()
    {
        return new GearSystemState
        {
            NeedInitialRampUp = false,
            GearGauge = 0,
            GearLevel = 0,
            GearGaugeGainCoefficient = new CharacterStat(1f),
        };
    }
}
