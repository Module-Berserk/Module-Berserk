using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ObjectExistenceSceneState))]
public class DestructibleObjects : MonoBehaviour, IDestructible
{
    [SerializeField] private float maxHP;
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private FlashEffectOnHit flashEffectOnHit;

    private CharacterStat hp;
    private CharacterStat defense = new(10f);

    private void Start()
    {
        hp = new CharacterStat(maxHP, 0f, maxHP);
    }

    CharacterStat IDestructible.GetDefenseStat()
    {
        return defense;
    }

    CharacterStat IDestructible.GetHPStat()
    {
        return hp;
    }

    Team IDestructible.GetTeam()
    {
        return Team.Environment;
    }

    bool IDestructible.OnDamage(AttackInfo attackInfo)
    {
        flashEffectOnHit.StartEffectAsync().Forget();

        transform.DOShakePosition(duration: 0.2f, strength: 0.1f, vibrato: 30);

        (this as IDestructible).HandleHPDecrease(attackInfo.damage);

        // 체력바 (최초 피격시 활성화)
        if (!healthBarSlider.gameObject.activeInHierarchy)
        {
            healthBarSlider.gameObject.SetActive(true);
        }
        healthBarSlider.value = hp.CurrentValue / hp.MaxValue;

        return true;
    }

    void IDestructible.OnDestruction()
    {
        GetComponent<ObjectExistenceSceneState>().RecordAsDestroyed();

        Destroy(gameObject);
    }
}
