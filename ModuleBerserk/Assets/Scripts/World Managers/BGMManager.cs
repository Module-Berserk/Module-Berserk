using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    [SerializeField]
    [Range(0, 100)]
    private int volume = 0;

    private AudioSource audioSource;
    [SerializeField] private AudioClip[] audioClips;
    private int currentTrackId = 0;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = audioClips[currentTrackId];
        //audioSource.loop = true;
        audioSource.volume = volume / 100;
        PlayBGM();
    }
    
    private void Update()
    {
        audioSource.volume = volume;
        //TODO: 브금 바꾸기 ㅇㅅㅇ
        if (!audioSource.isPlaying) {
            currentTrackId = 1 - currentTrackId;
            ChangeBGM(currentTrackId);
        }
    }

    public void ChangeBGM(int bgmId) {
        audioSource.Stop();
        audioSource.clip = audioClips[bgmId];
        currentTrackId = bgmId;
        PlayBGM();
    }
    public void PlayBGM() {
        audioSource.Play();
    }
}

