using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace MarsFPSKit
{
    namespace UI
    {
        [CreateAssetMenu(menuName = "MarsFPSKit/Options/Graphics/Anisotropic Filtering")]
        public class Kit_OptionsTextureAnisotropicFiltering : Kit_OptionBase
        {
            public LocalizedString[] valueToString;

            public override OptionType GetOptionType()
            {
                return OptionType.Dropdown;
            }

            public override void OnDropdownStart(TextMeshProUGUI txt, TMP_Dropdown dropdown)
            {
                //Load
                int selected = PlayerPrefs.GetInt("anisotropicFiltering", 2);
                //Clamp
                selected = Mathf.Clamp(selected, 0, 2);
                //Clear
                dropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < valueToString.Length; i++)
                {
                    options.Add(valueToString[i].GetLocalizedString());
                }
                //Add
                dropdown.AddOptions(options);

                //Set default value
                dropdown.value = selected;
                //Use that value
                OnDropdowChange(txt, selected);
            }

            public override void OnDropdowChange(TextMeshProUGUI txt, int newValue)
            {
                //Set
                QualitySettings.anisotropicFiltering = (AnisotropicFiltering)newValue;
                //Save
                PlayerPrefs.SetInt("anisotropicFiltering", newValue);
            }
        }
    }
}