using System;

[Serializable]
public class GearSystemState
{
    public float GearGauge;
    public int GearLevel;
    public CharacterStat GearGaugeGainCoefficient;

    public static GearSystemState CreateDummyState()
    {
        return new GearSystemState
        {
            GearGauge = 0,
            GearLevel = 0,
            GearGaugeGainCoefficient = new CharacterStat(1f),
        };
    }
}
