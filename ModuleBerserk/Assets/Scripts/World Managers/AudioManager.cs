using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager instance = null;

    [SerializeField] private AudioClip[] sfxList;
    private PlayerManager player;

    [Range(0, 100)]
    public int volume = 100;

    private List<AudioSourceByDistance> audioSources = new List<AudioSourceByDistance>();

    private void Awake() {
        instance = this;
        player = GameObject.FindWithTag("Player").GetComponent<PlayerManager>();
    }

    private void Update() {
        // for (int i = 0; i < audioSources.Count; i++) {
            
        //     if (audioSources[i].volume <= 0) {
        //         Destroy(audioSources[i]);
        //     }
        // } Audio Source Recycling
        for (int i = 0; i < audioSources.Count; i++) {
            if (audioSources[i].audioSource.volume > 0) {
                audioSources[i].audioSource.volume = CalculateVolume(audioSources[i].targetTransform);
            }
        }
    }

    private AudioSource GetAvailableAudioSource(Transform transform) {
        foreach (var e in audioSources) {
            if (!e.audioSource.isPlaying) {
                return e.audioSource;
            }
        }
        // If no available audio source, create a new one
        AudioSource newAudioSource = gameObject.AddComponent<AudioSource>();
        newAudioSource.playOnAwake = false;
        audioSources.Add(new AudioSourceByDistance(transform, newAudioSource));
        return newAudioSource;
    }

    public AudioSource PlaySFX(int[] indices) { 
        int randomIndex = indices[Random.Range(0, indices.Length)];
        AudioSource audioSource = GetAvailableAudioSource(player.transform);
        audioSource.volume = volume / 100f / 2f; //Max Volume = 0.5
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.clip = sfxList[randomIndex];
        audioSource.Play();
        return audioSource;
    }

    public AudioSource PlaySFXBasedOnPlayer(int[] indices, Transform target) {
        int randomIndex = indices[Random.Range(0, indices.Length)];
        float distance = Vector2.Distance(target.position, player.transform.position);
        AudioSource audioSource = GetAvailableAudioSource(target);
        audioSource.volume = CalculateVolume(target) * volume / 100f / 2f; //Max Volume = 0.5
        if (volume <= 0) {
            return null;
        }
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.clip = sfxList[randomIndex];
        audioSource.Play();
        return audioSource;
    }

    private float CalculateVolume(Transform target){
        float distance = Vector2.Distance(target.position, player.transform.position);
        return Mathf.Clamp01((20f - distance * 1.5f) / 20f);
    }

    public void StopSFX(AudioSource audioSource) {
        audioSource.Stop();
    }
}

