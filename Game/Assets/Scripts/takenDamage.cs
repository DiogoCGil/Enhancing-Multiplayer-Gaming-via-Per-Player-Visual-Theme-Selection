using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Audio;

public class takenDamage : MonoBehaviour
{
    public float intensity = 0;
        
    PostProcessVolume _volume;
    Vignette _vignette;

    private float healAmount = 0.075f;
    private float effectAmount = 0.25f;
    public AudioClip audioClip;
    private AudioSource audioSource;
    public AudioMixerGroup outputMixerGroup;
    private int previusInt = 0;
    void Start()
    {
        _volume = GetComponent<PostProcessVolume>();
        _volume.profile.TryGetSettings<Vignette>(out _vignette);
        audioSource = gameObject.AddComponent<AudioSource>();
        
        audioSource.clip = audioClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;

        if(!_vignette)
        {
            print("error");
        }
        else
        {
            _vignette.enabled.Override(false);
        }
    }

    private bool isPlayerDead = false;

    void Update()
    {
        if (isPlayerDead) return;

        if (intensity > 0f)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();

            audioSource.loop = true;
            audioSource.volume = Mathf.Clamp01(intensity -0.15f);
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }

    public void TakeDamageEffect()
    {
        intensity += effectAmount;
        intensity = Mathf.Clamp01(intensity);
        _vignette.enabled.Override(true);
        _vignette.intensity.Override(intensity);
        

    }
    public void HealDamage()
    {   
        intensity -= healAmount;

        if (intensity < 0.01f)
        {
            intensity = 0f;
        }


        _vignette.enabled.Override(true);
        _vignette.intensity.Override(intensity);
        
    }
    public void RemoveEffect()
    {
        intensity = 0f;
        isPlayerDead = true;


        _vignette.enabled.Override(true);
        _vignette.intensity.Override(intensity);

        audioSource.Stop();
        audioSource.loop = false;
        audioSource.volume = 0f;
    }
    public void AdjustEffect(int receive)
    {  
        if (previusInt != receive)
        {
            effectAmount = receive / 80f;                  
            previusInt = receive;
        }
    }
}
