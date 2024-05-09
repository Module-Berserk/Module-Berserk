using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 데미지를 입으면 스프라이트가 잠깐 반짝이는 피격 이펙트를 담당하는 클래스
public class FlashEffectOnHit : MonoBehaviour
{
    // 이펙트가 지속되는 동안 적용할 material
    //
    // note:
    // 지금은 Assets/Material/FlashEffectMaterial 사용하는데
    // 원한다면 색을 바꾸거나 아예 다른 material을 써도 상관 없음
    [SerializeField] private Material flashEffectMaterial;
    // 이펙트의 지속 시간
    [SerializeField] private float flashDuration = 0.15f;

    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;

    // 이펙트 진행 도중에 오브젝트가 삭제되는 상황에 대처하기 위한 token
    private CancellationTokenSource cancellationTokenSource = new();

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalMaterial = spriteRenderer.material;
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴되면 내가 참조하는 SpriteRederer도 삭제되므로
        // 진행중인 StartEffectAsync에서 레퍼런스 오류 발생함!
        // 이를 막기 위해 아직 끝나지 않은 UniTask를 종료.
        cancellationTokenSource.Cancel();
    }

    public async UniTask StartEffectAsync()
    {
        spriteRenderer.material = flashEffectMaterial;

        // await 도중에 오브젝트가 파괴되어 spriteRenderer 참조가
        // 유효하지 않게 될 수 있으므로 cancellationToken으로 중단 가능하게 만듦.
        await UniTask.WaitForSeconds(flashDuration, cancellationToken: cancellationTokenSource.Token);

        spriteRenderer.material = originalMaterial;
    }
}
