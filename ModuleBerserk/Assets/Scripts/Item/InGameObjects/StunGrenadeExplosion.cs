using UnityEngine;

public class StunGrenadeExplosion : MonoBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private float stunDuration;

    private void Start()
    {
        var hitbox = GetComponent<Hitbox>();
        hitbox.BaseDamage = new CharacterStat(damage);
    }
    
    // 영역에 들어오면 기절
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IStunnable stunnable))
        {
            stunnable.GetStunnedForDuration(stunDuration);
        }
    }
}
