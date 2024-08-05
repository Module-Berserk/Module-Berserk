using UnityEngine;

// 플레이어와 적 모두 접촉하는 즉시 사망하는 낙사 구간.
// 강한 데미지를 주는 동시에 플레이어의 조작을 막아서
// 충격파 사용을 통한 데미지 무효화 등의 파훼법을 원천봉쇄한다.
[RequireComponent(typeof(PlayerContactTrigger))]
public class FallDamageArea : InitializeHitboxBaseDamage
{
    private void Awake()
    {
        // 플레이어가 낙사 구간에 들어오면 충격파 등으로 살아날 수 없도록 아예 조작을 막아버림!
        GetComponent<PlayerContactTrigger>().OnActivate.AddListener(()=>{
            FindObjectOfType<PlayerManager>().enabled = false;
        });
    }
}
