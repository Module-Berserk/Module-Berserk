using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 플레이어의 무기 또는 적의 공격 콜라이더와 접촉할 때
// 데미지를 입히기 위한 테스트용 컴포넌트!
//
// TODO: 테스트 끝나고 필요 없어지면 삭제할 것
public class ApplyDamageOnContact : MonoBehaviour
{
    // 테스트용으로 만든거라 그냥 public으로 노출했음.
    // 공격력 스탯을 가져와서 적용하지 않고 그냥 인스펙터에서 넣어준 값을 그대로 사용함.
    //
    // TODO:
    // 실제 공격 판정 구현할 때는 짧은 시간 안에 TriggerEnter와 TriggerExit을 반복해
    // 중복으로 데미지를 입히는 경우가 없도록 edge case를 잘 처리해줘야 함!!!
    public float RawDamage;
    public Team DamageSource;
    public StaggerStrength staggerStrength;
    public UnityEvent OnApplyDamageSuccess;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IDestructible destructible))
        {
            // 공격 대상이 나보다 왼쪽에 있으면 경직 방향도 왼쪽으로 설정.
            Vector2 staggerDirection = other.transform.position.x < transform.position.x ? Vector2.left : Vector2.right;
            StaggerInfo staggerInfo = new(staggerStrength, staggerDirection);

            // 공격에 성공했다면 이벤트로 알려줌 (ex. 공격 성공 시 기어 게이지 상승)
            if (destructible.TryApplyDamage(DamageSource, RawDamage, staggerInfo))
            {
                OnApplyDamageSuccess.Invoke();
            }
        }
    }
}
