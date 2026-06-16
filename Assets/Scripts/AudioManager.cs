using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private Slider soundVolume;
    private static float currentSoundVol= 0.5f;
    [SerializeField] private Slider musicVolume;
    private static float currentMusicVol= 0.5f;
    [SerializeField] private GameObject backgroundMusic;
    private AudioDistortionFilter distortion;
    private AudioSource musicSource;
    
    [SerializeField] private AudioSource dashSound;
    [SerializeField] private AudioSource reflectSound;
    [SerializeField] private AudioSource warpSound;
    [SerializeField] private AudioSource lowCollideSound;
    [SerializeField] private AudioSource highCollideSound;
    
    [SerializeField] private float stabilizeRate = 3f;
    
    public static AudioManager Instance;

    private float intensity=1f;

    private void Awake()
    {
        Instance = this;
        distortion = backgroundMusic.GetComponent<AudioDistortionFilter>();
        musicSource = backgroundMusic.GetComponent<AudioSource>();

        soundVolume.onValueChanged.AddListener(OnSoundValueChange);
        musicVolume.onValueChanged.AddListener(OnMusicValueChange);
    }

    void Start()
    {
        soundVolume.value = currentSoundVol;
        musicVolume.value = currentMusicVol;
        
        OnSoundValueChange(soundVolume.value);
        OnMusicValueChange(musicVolume.value);
    }

    void OnSoundValueChange(float value)
    {
        currentSoundVol = value;
        dashSound.volume        = value;
        reflectSound.volume     = value;
        warpSound.volume        = value;
        lowCollideSound.volume  = value;
        highCollideSound.volume = value;
    }

    void OnMusicValueChange(float value)
    {
        currentMusicVol = value;
        if (musicSource != null)
            musicSource.volume = value;
    }
    
    void Update()
    {
        //intensity = Mathf.Max(musicVolume, intensity - Time.deltaTime * stabilizeRate);
        
        intensity = Mathf.MoveTowards(
            intensity,
            0f,
            Time.deltaTime * 0.2f
        );

        //distortion.distortionLevel = intensity;
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
