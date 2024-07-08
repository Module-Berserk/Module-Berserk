using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    [SerializeField]
    [Range(0, 100)]
    private int volume = 100;

    private AudioSource audioSource;
    [SerializeField] private AudioClip[] audioClips;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = audioClips[0];
        audioSource.loop = true;
        audioSource.volume = volume / 100;
        audioSource.Play();
    }
    
    private void Update()
    {
        audioSource.volume = volume;
        //TODO: 브금 바꾸기 ㅇㅅㅇ
    }

    public void ChangeBGM() {
        audioSource.Stop();
        Debug.Log("stop");
        audioSource.clip = audioClips[1];
    }
    public void playBGM() {
        audioSource.Play();
    }
}

