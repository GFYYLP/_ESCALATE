using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource dashSound;
    [SerializeField] private AudioSource reflectSound;
    [SerializeField] private AudioSource warpSound;
    [SerializeField] private AudioSource highCollideSound;
    
    public static AudioManager Instance;

    private void Awake()
    {
        Instance = this;
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
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
