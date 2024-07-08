using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class C1BGMManager : MonoBehaviour
{
    [SerializeField]
    [Range(0, 100)]
    private int volume = 100;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.volume = volume / 100;
        audioSource.Play();
    }
    
    private void Update()
    {
        audioSource.volume = volume;
        //TODO: 브금 바꾸기 ㅇㅅㅇ
    }
}

