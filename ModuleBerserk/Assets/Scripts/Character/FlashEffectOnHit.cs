using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

// 데미지를 입으면 스프라이트가 잠깐 반짝이는 피격 이펙트를 담당하는 클래스
// SpriteRenderer와 Image 두 종류 모두 지원한다.
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
    private Image image;
    private Material originalMaterial;

    // 이펙트 진행 도중에 오브젝트가 삭제되는 상황에 대처하기 위한 token
    private CancellationTokenSource cancellationTokenSource = new();

    private Material activeMaterial
    {
        get
        {
            if (spriteRenderer != null)
            {
                return spriteRenderer.material;
            }
            else if (image != null)
            {
                return image.material;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        set
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.material = value;
            }
            else if (image != null)
            {
                image.material = value;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    private void Awake()
    {
        // 둘 중에 뭘 사용해야 할지 모르니 일단 가져와보고 null인지 확인
        spriteRenderer = GetComponent<SpriteRenderer>();
        image = GetComponent<Image>();

        originalMaterial = activeMaterial;
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
        activeMaterial = flashEffectMaterial;

        // await 도중에 오브젝트가 파괴되어 spriteRenderer 참조가
        // 유효하지 않게 될 수 있으므로 cancellationToken으로 중단 가능하게 만듦.
        await UniTask.WaitForSeconds(flashDuration, cancellationToken: cancellationTokenSource.Token);

        activeMaterial = originalMaterial;
    }
}
