using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 플레이어의 무기 또는 적의 공격 콜라이더와 접촉할 때 데미지를 입히는 컴포넌트.
// 대칭적인 모양의 Collider2D가 필요하고 layer는 Weapon으로 설정해야 한다.
//
// 플레이어의 공격처럼 상황마다 다른 모양의 콜라이더를 사용하는 경우를 고려해
// 하나의 오브젝트에 여러 개의 콜라이더와 하나의 ApplyDamageOnContact 스크립트가 있다고 가정함.
// 콜라이더 중에서는 한 번에 하나만 활성화되는 것이 정상!
//
// object hierarchy 예시:
//
//    Player
//    ├─ ApplyDamageOnHit   <-- kinematic rigidbody, layer는 Weapon
//    │  ├─ Collider1       <-- 1타 공격의 범위에 맞는 콜라이더, layer는 Weapon
//    │  ├─ Collider2       <-- 2타 공격의 범위에 맞는 콜라이더, layer는 Weapon
//
//    1타 모션의 애니메이션 클립은 Collider1.enabled를 토글,
//    2타 모션의 애니메이션 클립은 Collider2.enabled를 토글, ...
//
// 콜라이더를 하나만 쓴다면 kinematic rigidbody 없이
// ApplyDamageOnHit 스크립트 붙은 자식 오브젝트에
// 바로 콜라이더 하나만 달아주면 된다
//
// 주의사항:
// 모든 콜라이더의 위치 조정은 transform.position이 아니라 offset으로 해줘야
// 왼쪽 오른쪽 방향 전환이 정상적으로 동작함!
public class ApplyDamageOnContact : MonoBehaviour
{
    [SerializeField] private Team DamageSource;
    [SerializeField] private StaggerStrength staggerStrength;
    [SerializeField] private float knockbackForce;
    [SerializeField] private float staggerDuration = 0.5f;
    // 콜라이더가 오래 켜있는 지속 데미지의 경우 몇 초마다 데미지를 입힐 수 있는지 결정.
    [SerializeField] private float delayBetweenDamageTick = 0.1f;

    // Note: 공격 모션마다 다른 히트박스 범위를 원하는 경우
    // 여러 콜라이더를 만들어두고 그 중에 하나를 활성화하는 방식을 사용함
    private Collider2D[] hitboxes;

    // 최근에 데미지를 입힌 대상이 다시 데미지를 입기까지 몇 초나 남았는지 기록.
    // 장판 패턴처럼 긴 시간동안 반복적으로 데미지를 입히는 경우에 매우 중요한 역할을 맡는다.
    private Dictionary<IDestructible, float> recentDamages = new();

    // 넉백 방향을 결정할 때 참고하는 플래그.
    // SetHitboxDirection()을 호출할 때마다 변경된다.
    private bool isHitboxFacingLeft = false;

    public bool IsHitboxEnabled
    {
        get
        {
            // 하나라도 켜있으면 true
            foreach (Collider2D hitbox in hitboxes)
            {
                if (hitbox.enabled) return true;
            }
            return false;
        }
        set
        {
            // 전체를 활성화하거나 비활성화.
            // 플레이어처럼 여러 콜라이더 중 하나만 활성화하는 경우는
            // 애니메이션 클립에서 직접 골라서 처리한다.
            foreach (Collider2D hitbox in hitboxes)
            {
                hitbox.enabled = value;
            }
        }
    }

    // 공격의 주체가 설정해줘야하는 스탯
    public CharacterStat RawDamage;

    // 공격마다 계수가 다른 경우 설정
    public float DamageCoefficient = 1f;

    // 공격 성공 여부가 필요한 경우 사용할 수 있는 이벤트.
    // 기어 시스템에서 게이지를 회복하는 조건으로 활용한다.
    public UnityEvent<Collider2D> OnApplyDamageSuccess = new();

    private void Awake()
    {
        hitboxes = GetComponentsInChildren<Collider2D>();
    }

    public void SetHitboxDirection(bool isFacingLeft)
    {
        isHitboxFacingLeft = isFacingLeft;

        // 콜라이더가 대칭적인 형태라고 가정하고
        // 바라보는 방향에 따라 콜라이더 위치 조정
        foreach (Collider2D hitbox in hitboxes)
        {
            float newOffsetX = Mathf.Abs(hitbox.offset.x) * (isFacingLeft ? -1f : 1f);
            hitbox.offset = new Vector2(newOffsetX, hitbox.offset.y);
        }
    }

    private void FixedUpdate()
    {
        if (recentDamages.Count > 0)
        {
            // 최근에 데미지를 입은 대상들을 순회하며 데미지 틱 간격이 지났는지 확인한다.
            // foreach 루프에서는 딕셔너리를 직접 수정할 수 없기 때문에 keys를 복사해 사용함.
            List<IDestructible> keys = new(recentDamages.Keys);
            foreach (IDestructible destructible in keys)
            {
                recentDamages[destructible] -= Time.fixedDeltaTime;

                // 다시 데미지를 입힐 수 있을 만큼 시간이 지난 대상은 recentDamages 목록에서 제거한다.
                if (recentDamages[destructible] < 0f)
                {
                    recentDamages.Remove(destructible);
                }
            }
        }
    }

    // 충돌이 일어난 프레임에는 Ontriggerstay 이벤트가 없는지
    // 데미지가 안 들어가서 enter와 stay를 둘 다 활용하는 방법으로 구현함.
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryApplyTickDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryApplyTickDamage(other);
    }

    private void TryApplyTickDamage(Collider2D other)
    {
        if (other.TryGetComponent(out IDestructible destructible))
        {
            // 이미 체력이 다 닳은 상대는 공격 불가
            if (destructible.GetHPStat().CurrentValue <= 0f)
            {
                return;
            }

            // 데미지 틱 간격 안에는 다시 데미지를 입히지 않음.
            if (!recentDamages.ContainsKey(destructible))
            {
                // 이제 delayBetweenDamageTick만큼 시간이 지날 때까지 이 대상에게 데미지를 입히지 않음
                recentDamages.Add(destructible, delayBetweenDamageTick);

                // 넉백 방향은 무조건 공격한 사람이 바라보는 방향 기준으로 들어감!
                Vector2 knockbackDirection = isHitboxFacingLeft ? Vector2.left : Vector2.right;
                AttackInfo attackInfo = new()
                {
                    damageSource = DamageSource,
                    damage = RawDamage.CurrentValue * DamageCoefficient,
                    staggerStrength = staggerStrength,
                    knockbackForce = knockbackDirection * knockbackForce,
                    duration = staggerDuration
                };

                // 공격에 성공했다면 이벤트로 알려줌 (ex. 공격 성공 시 기어 게이지 상승)
                if (destructible.TryApplyDamage(attackInfo))
                {
                    OnApplyDamageSuccess.Invoke(other);

                    // 나중에 다단 히트 공격이 의도된 횟수만큼 타격하는지
                    // 확인할 때 로그가 필요할 수 있어서 주석으로 남겨둠!
                    // Debug.Log("공격 성공");
                }
            }
        }
    }
}
