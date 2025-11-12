using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Audio_SFX  : MonoBehaviour
{
    [SerializeField] Slider soundSlider;
    [SerializeField] AudioMixer masterMixar;

    void Start()
    {
        SetVolume(PlayerPrefs.GetFloat("SavedMasterVolume", 100));   
    }
    public void SetVolume(float _value)
    {
        if(_value < 1){
            _value = .001f;
        };
        RefresSlider(_value);
        PlayerPrefs.SetFloat("SavedMasterVolume", _value);
        masterMixar.SetFloat("MainVolume", Mathf.Log10(_value/100)* 20f);
    }
    public void SetVolumeFromSlider(){
        SetVolume(soundSlider.value);
    }


    public void RefresSlider(float _value){
        soundSlider.value = _value;
    }
}
