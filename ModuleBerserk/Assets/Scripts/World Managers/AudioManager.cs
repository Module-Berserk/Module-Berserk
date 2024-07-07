using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager instance = null;

    [SerializeField] private AudioClip[] sfxList;
    [SerializeField] private int initialAudioSourceCount = 10; // Number of initial AudioSources in the pool

    [Range(0, 100)]
    public int volume = 100;

    private List<AudioSource> audioSources = new List<AudioSource>();

    private void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
            InitializeAudioSources();
        } else {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources() {
        for (int i = 0; i < initialAudioSourceCount; i++) {
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSources.Add(audioSource);
        }
    }

    private AudioSource GetAvailableAudioSource() {
        foreach (var audioSource in audioSources) {
            if (!audioSource.isPlaying) {
                return audioSource;
            }
        }
        // If no available audio source, create a new one
        AudioSource newAudioSource = gameObject.AddComponent<AudioSource>();
        newAudioSource.playOnAwake = false;
        audioSources.Add(newAudioSource);
        return newAudioSource;
    }

    public void PlaySFX(int[] indices) { 
        int randomIndex = indices[Random.Range(0, indices.Length)];
        AudioSource audioSource = GetAvailableAudioSource();
        audioSource.volume = volume / 100f;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.clip = sfxList[randomIndex];
        audioSource.Play();
    }
}

