using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // 1. Added for AudioMixer

namespace MarsFPSKit
{
    namespace UI
    {
        [CreateAssetMenu(menuName = "MarsFPSKit/Options/Audio/Master Volume")]
        public class Kit_OptionsMasterVolume : Kit_OptionBase
        {
            [Header("Audio Mixer Settings")]
            public AudioMixer targetMixer;
            public string exposedParameterName = "SFXVolume";

            public override OptionType GetOptionType()
            {
                return OptionType.Slider;
            }

            public override void OnSliderStart(TextMeshProUGUI txt, Slider slider)
            {
                float load = PlayerPrefs.GetFloat("audioVolume", 1f);
                load = Mathf.Clamp(load, 0, 1);

                slider.minValue = 0;
                slider.maxValue = 1;

                slider.value = load;
                OnSliderChange(txt, load);
            }

            public override void OnSliderChange(TextMeshProUGUI txt, float newValue)
            {
                AudioListener.volume = newValue;
                PlayerPrefs.SetFloat("audioVolume", newValue);

                if (targetMixer != null)
                {
                    float mixerValue = Mathf.Clamp(newValue, 0.0001f, 1f);
                    float decibels = Mathf.Log10(mixerValue) * 20f;
                    targetMixer.SetFloat(exposedParameterName, decibels);
                }

                txt.text = GetDisplayName() + ": " + (newValue * 100f).ToString("F0") + "%";
            }
        }
    }
}