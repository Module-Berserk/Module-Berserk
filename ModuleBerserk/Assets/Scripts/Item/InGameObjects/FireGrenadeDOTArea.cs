using UnityEngine;

public class FireGrenadeDOTArea : MonoBehaviour
{
    [SerializeField] private float tickDamage = 2f;
    [SerializeField] private float duration = 1.1f; // 0.2초 틱이 안정적으로 5번 들어갈 시간

    private float lifetime = 0;

    private void Start()
    {
        var hitbox = GetComponent<ApplyDamageOnContact>();
        hitbox.RawDamage = new CharacterStat(tickDamage);
        //SFX
        int[] flameIndices = {37};
        AudioManager.instance.PlaySFX(flameIndices);
    }

    private void Update()
    {
        lifetime += Time.deltaTime;
        if (lifetime > duration)
        {
            Destroy(gameObject);
        }
    }
}
