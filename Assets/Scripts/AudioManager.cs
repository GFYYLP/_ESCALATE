using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private GameObject backgroundMusic;
    private AudioDistortionFilter distortion;
    
    [SerializeField] private AudioSource dashSound;
    [SerializeField] private AudioSource reflectSound;
    [SerializeField] private AudioSource warpSound;
    [SerializeField] private AudioSource lowCollideSound;
    [SerializeField] private AudioSource highCollideSound;
    
    [SerializeField] private float musicVolume = 3f;
    [SerializeField] private float stabilizeRate = 3f;
    
    public static AudioManager Instance;

    private float intensity=1f;

    private void Awake()
    {
        Instance = this;
        distortion = backgroundMusic.GetComponent<AudioDistortionFilter>();
    }

    void Start()
    {
    }
    
    void Update()
    {
        //intensity = Mathf.Max(musicVolume, intensity - Time.deltaTime * stabilizeRate);
        
        intensity = Mathf.MoveTowards(
            intensity,
            0f,
            Time.deltaTime * 0.2f
        );

        distortion.distortionLevel = intensity;
    }

    public void DistortMusic(float strength)
    {
        intensity = Mathf.Clamp(
            intensity + strength, 0, 0.7f
        );
    }

    public void PlayDashSound()
    {
        if (dashSound != null)
            dashSound.Play();
    }
    public void PlayReflectSound()
    {
        if (reflectSound != null)
            reflectSound.Play();
    }
    public void PlayWarpSound()
    {
        if (warpSound != null)
            warpSound.Play();
    }

    public void PlayHighCollideSound()
    {
        if (highCollideSound != null)
            highCollideSound.Play();
    }
    public void PlayLowCollideSound()
    {
        if (lowCollideSound != null)
            lowCollideSound.Play();
    }
}
