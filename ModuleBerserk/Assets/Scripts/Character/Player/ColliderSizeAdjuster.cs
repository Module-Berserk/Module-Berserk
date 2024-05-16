using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 점프할 때 idle 상태에 맞는 콜라이더 크기가 유지되어
// 실제 캐릭터보다 더 큰 범위가 피격 판정에 들어가는 문제를 해결하기 위해
// 임시 방편으로 점프 도중에만 콜라이더 크기를 살짝 줄여주는 컴포넌트를 사용.
public class ColliderSizeAdjuster : MonoBehaviour {
    private BoxCollider2D boxCollider;

    private bool sizeAdjusted = false;

    private void Awake() {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    public void EnableAutoUpdateColliderSize() {
        if (!sizeAdjusted){
            boxCollider.size *= 0.8f;
            sizeAdjusted = true;
        }

    }

    public void DisableAutoUpdateColliderSize() {
        if (sizeAdjusted){
            boxCollider.size /= 0.8f;
            sizeAdjusted = false;
        }
    }

    // private void LateUpdate() {
    //     if (autoUpdateColliderSize) {
    //         UpdateColliderSize();
    //         Debug.Log(spriteRenderer.sprite.height);
    //     }
    // }
}