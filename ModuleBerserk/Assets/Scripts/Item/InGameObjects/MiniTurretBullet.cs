using UnityEngine;

public class MiniTurretBullet : MonoBehaviour
{
    [SerializeField] private float damage;

    private void Start()
    {
        var hitbox = GetComponent<ApplyDamageOnContact>();
        hitbox.RawDamage = new CharacterStat(damage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}
