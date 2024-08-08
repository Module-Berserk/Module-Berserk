using UnityEngine;

public class StatRandomizer : MonoBehaviour
{
    [SerializeField] private float randomizationFactor = 0.1f;

    public float SampleRandomizationFactor()
    {
        return Random.Range(1 + randomizationFactor, 1 - randomizationFactor);
    }
}
