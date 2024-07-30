using UnityEngine;

[RequireComponent(typeof(ApplyDamageOnContact))]
public class InitializeHitboxBaseDamage : MonoBehaviour
{
    [SerializeField] private float damage;

    private void Start()
    {
        var hitbox = GetComponent<ApplyDamageOnContact>();
        hitbox.RawDamage = new CharacterStat(damage);
    }
}
