using System;

[Serializable]
public class GearSystemState
{
    public float GearGauge {get; set;}
    public int GearLevel {get; set;}

    public static GearSystemState CreateDummyState()
    {
        return new GearSystemState
        {
            GearGauge = 0,
            GearLevel = 0,
        };
    }
}
