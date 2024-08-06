using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AudioSourceByDistance {
    public Transform targetTransform;
    public AudioSource audioSource;

    public AudioSourceByDistance(Transform targetTransform, AudioSource audioSource) {
        this.targetTransform = targetTransform;
        this.audioSource = audioSource;
    }
}
