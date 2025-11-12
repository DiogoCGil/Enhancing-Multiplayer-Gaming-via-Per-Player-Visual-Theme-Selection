using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.Audio;


public class StaminaController : MonoBehaviour
{
    [Header("Stamina Main Parameters")]
    public float playerStamina = 100.0f;
    [SerializeField] private float maxStamina = 100.0f;
    [SerializeField] private float jumpCost = 25;
    [HideInInspector] public bool hasRegemerated = true;
    [HideInInspector] public bool weAreSprinting = false;

    [Header("Stamina Regen Parameters")]
    [Range(0,50)] [SerializeField] private float staminaDrain = 10f;
    [Range(0,50)] [SerializeField] private float staminaRegen = 30f;

    [Header("Stamina Speed Parameters")]
    [SerializeField] private int slowedRunSpeed = 4;
    [SerializeField] private int normalRunSpeed = 8;

    [Header("Stamina UI Elements")]
    [SerializeField] public Image staminaProgressUI = null;

    private FirstPersonControllerCostum playerController;

    public AudioClip audioClip;

    private AudioSource audioSource;
    public AudioMixerGroup outputMixerGroup;
    private void Start()
    {
        playerController = GetComponent<FirstPersonControllerCostum>();
        GameObject X = GameObject.FindGameObjectWithTag("StaminaBar");
        if(X != null)
        {
            staminaProgressUI = X.GetComponent<Image>();
            UpdateStamina();
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.loop = false;
        audioSource.volume = 0.2f;
        audioSource.playOnAwake = false;
        audioSource.outputAudioMixerGroup = outputMixerGroup; 
    }

    private void Update()
    {
        if(!weAreSprinting){
            if(playerStamina <= maxStamina - 0.01)
            {
                playerStamina += staminaRegen * Time.deltaTime;
                UpdateStamina();
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
                
                float epsilon = 0.1f;
                if (playerStamina >= maxStamina - epsilon) 
                {  
                    if (audioSource.isPlaying)
                    {
                        audioSource.Stop();
                        audioSource.time = 0f;
                    }
                    playerController.SetRunSpeed(normalRunSpeed);
                    hasRegemerated = true;
                }
            }
        }
    }
    public void Sprinting()
    {
        if(hasRegemerated)
        {
            weAreSprinting = true;
            playerStamina -= staminaDrain* Time.deltaTime;
            UpdateStamina();
            if(playerStamina <=0){
                hasRegemerated= false;
                playerController.SetRunSpeed(slowedRunSpeed);
            }
        }
    }
    public void StaminaJump()
    {
        if(playerStamina >=( maxStamina * jumpCost / maxStamina))
        {
            playerStamina -= jumpCost;
            playerController.PlayerJump();
            UpdateStamina();
        } 
    }
    void UpdateStamina()
    {
        staminaProgressUI.fillAmount = playerStamina / maxStamina;
    
    }
    
}   
