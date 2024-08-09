using UnityEngine;

[RequireComponent(typeof(Hitbox))]
public class InitializeHitboxBaseDamage : MonoBehaviour
{
    [SerializeField] private float damage;

    private void Start()
    {
        var hitbox = GetComponent<Hitbox>();
        hitbox.BaseDamage = new CharacterStat(damage);
    }
}
