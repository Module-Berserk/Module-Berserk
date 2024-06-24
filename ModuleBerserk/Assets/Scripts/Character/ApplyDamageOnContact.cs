using UnityEngine;
using UnityEngine.Events;

// 플레이어의 무기 또는 적의 공격 콜라이더와 접촉할 때 데미지를 입히는 컴포넌트.
// 대칭적인 모양의 Collider2D가 필요하고 layer는 Weapon으로 설정해야 한다.
//
// TODO:
// 실제 공격 판정 구현할 때는 짧은 시간 안에 TriggerEnter와 TriggerExit을 반복해
// 중복으로 데미지를 입히는 경우가 없도록 edge case를 잘 처리해줘야 함!!!
[RequireComponent(typeof(Collider2D))]
public class ApplyDamageOnContact : MonoBehaviour
{
    [SerializeField] private Team DamageSource;
    [SerializeField] private StaggerStrength staggerStrength;

    public bool IsHitboxEnabled
    {
        get => hitbox.enabled;
        set => hitbox.enabled = value;
    }

    // 공격의 주체가 설정해줘야하는 스탯
    public CharacterStat RawDamage;

    // 공격마다 계수가 다른 경우 설정
    public float DamageCoefficient = 1f;

    // 공격 성공 여부가 필요한 경우 사용할 수 있는 이벤트.
    // 기어 시스템에서 게이지를 회복하는 조건으로 활용한다.
    public UnityEvent OnApplyDamageSuccess;

    private Collider2D hitbox;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
    }

    public void SetHitboxDirection(bool isFacingLeft)
    {
        // 콜라이더가 대칭적인 형태라고 가정하고
        // 바라보는 방향에 따라 콜라이더 위치 조정
        float newOffsetX = Mathf.Abs(hitbox.offset.x) * (isFacingLeft ? -1f : 1f);
        hitbox.offset = new Vector2(newOffsetX, hitbox.offset.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IDestructible destructible))
        {
            // 공격 대상이 나보다 왼쪽에 있으면 경직 방향도 왼쪽으로 설정.
            Vector2 staggerDirection = other.transform.position.x < transform.position.x ? Vector2.left : Vector2.right;
            StaggerInfo staggerInfo = new(staggerStrength, staggerDirection, 0.5f); // TODO: 경직 시간을 외부에서 설정할 수 있도록 수정

            // 공격에 성공했다면 이벤트로 알려줌 (ex. 공격 성공 시 기어 게이지 상승)
            if (destructible.TryApplyDamage(DamageSource, RawDamage.CurrentValue * DamageCoefficient, staggerInfo))
            {
                OnApplyDamageSuccess.Invoke();
            }
        }
    }
}
