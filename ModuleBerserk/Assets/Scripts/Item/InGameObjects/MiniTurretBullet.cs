using UnityEngine;

public class MiniTurretBullet : MonoBehaviour
{
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float damage;

    private void Start()
    {
        var hitbox = GetComponent<Hitbox>();
        hitbox.BaseDamage = new CharacterStat(damage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
