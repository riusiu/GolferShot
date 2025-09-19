using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SePlayer : MonoBehaviour
{
    [SerializeField] private AudioClip _audioClip;

    [SerializeField] private float _volume;
    private AudioSource _audioSource;
    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.volume = _volume;
        _audioSource.clip = _audioClip; 
    }

    public void PlaySe()
    {
        _audioSource.Play();
    }
}
