using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    // Instance
    public static AudioManager instance = null;

    // List of SFX
    [SerializeField] private AudioClip[] sfxList;

    // Volume
    [Range(0, 100)]
    public int volume = 100;

    private AudioSource audioSource;

    private void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        } else {
            Destroy(gameObject);
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    //Play SFX Function based on index given.
    public void PlaySFX(int[] indices) {
        int randomIndex = indices[Random.Range(0, indices.Length)];
        
        if (randomIndex < 0 || randomIndex >= sfxList.Length) {
            Debug.LogWarning("이게 나오면 자살해야함");
            return;
        }

        audioSource.volume = volume / 100f;
        audioSource.pitch = Random.Range(0.7f, 1.3f); //추후 적절한 값 찾을 예정
        audioSource.clip = sfxList[randomIndex];
        audioSource.Play();
    }
}
