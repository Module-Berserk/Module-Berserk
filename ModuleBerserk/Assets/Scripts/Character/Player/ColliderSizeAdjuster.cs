using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderSizeAdjuster : MonoBehaviour {
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    private bool sizeAdjusted = false;

    private void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
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



