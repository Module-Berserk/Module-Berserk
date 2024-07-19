using Cysharp.Threading.Tasks;

public class FuelAdditive : ActiveItemBase
{
    public override void Use()
    {
        GameStateManager
            .ActiveGameState
            .PlayerState
            .GearSystemState
            .GearGaugeGainCoefficient
            .ApplyMultiplicativeModifierForDurationAsync(1.5f, 60f).Forget();
    }
}