using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarsFPSKit
{
    namespace UI
    {
        [CreateAssetMenu(menuName = "MarsFPSKit/Options/Graphics/LOD Bias")]
        public class Kit_OptionsLODBias : Kit_OptionBase
        {
            /// <summary>
            /// Minimum Value
            /// </summary>
            public float minValue = 0.5f;
            /// <summary>
            /// Maximum Value
            /// </summary>
            public float maxValue = 2f;

            public override OptionType GetOptionType()
            {
                return OptionType.Slider;
            }

            public override void OnSliderStart(TextMeshProUGUI txt, Slider slider)
            {
                float load = PlayerPrefs.GetFloat("lodBias", 1f);
                load = Mathf.Clamp(load, minValue, maxValue);

                slider.minValue = minValue;
                slider.maxValue = maxValue;

                slider.value = load;
                OnSliderChange(txt, load);
            }

            public override void OnSliderChange(TextMeshProUGUI txt, float newValue)
            {
                QualitySettings.lodBias = newValue;
                PlayerPrefs.SetFloat("lodBias", newValue);
                txt.text = GetDisplayName() + ": " + newValue.ToString("F1");
            }
        }
    }
}