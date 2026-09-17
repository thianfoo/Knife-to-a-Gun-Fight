using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace MarsFPSKit
{
    namespace UI
    {
        [CreateAssetMenu(menuName = "MarsFPSKit/Options/Graphics/Shadow Resolution")]
        public class Kit_OptionsShadowResolution : Kit_OptionBase
        {
            public LocalizedString[] valueToString;

            public override OptionType GetOptionType()
            {
                return OptionType.Dropdown;
            }

            public override void OnDropdownStart(TextMeshProUGUI txt, TMP_Dropdown dropdown)
            {
                //Load
                int selected = PlayerPrefs.GetInt("shadowResolution", 3);
                //Clamp
                selected = Mathf.Clamp(selected, 0, 3);
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
                QualitySettings.shadowResolution = (ShadowResolution)newValue;
                //Save
                PlayerPrefs.SetInt("shadowResolution", newValue);
            }
        }
    }
}