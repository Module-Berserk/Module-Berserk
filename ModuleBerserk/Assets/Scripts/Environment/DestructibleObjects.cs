using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(FlashEffectOnHit))]
public class DestructibleObjects : MonoBehaviour, IDestructible
{
    [SerializeField] private float maxHP;

    private FlashEffectOnHit flashEffectOnHit;
    private CharacterStat hp;
    private CharacterStat defense = new(10f);

    private void Start()
    {
        flashEffectOnHit = GetComponent<FlashEffectOnHit>();

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

        return true;
    }

    void IDestructible.OnDestruction()
    {
        Destroy(gameObject);
    }
}
