using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace MarsFPSKit
{
    namespace UI
    {
        [CreateAssetMenu(menuName = "MarsFPSKit/Options/Graphics/Texture Quality")]
        public class Kit_OptionsTextureQuality : Kit_OptionBase
        {
            public LocalizedString[] textureQualitySettings;

            public override OptionType GetOptionType()
            {
                return OptionType.Dropdown;
            }

            public override void OnDropdownStart(TextMeshProUGUI txt, TMP_Dropdown dropdown)
            {
                //Load
                int selectedResolution = PlayerPrefs.GetInt("textureResolution", 0);
                //Clamp
                selectedResolution = Mathf.Clamp(selectedResolution, 0, textureQualitySettings.Length - 1);
                //Clear
                dropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < textureQualitySettings.Length; i++)
                {
                    options.Add(textureQualitySettings[i].GetLocalizedString());
                }
                //Add
                dropdown.AddOptions(options);
                //Set default value
                dropdown.value = selectedResolution;
                //Use that value
                OnDropdowChange(txt, selectedResolution);
            }

            public override void OnDropdowChange(TextMeshProUGUI txt, int newValue)
            {
                //Set
                QualitySettings.globalTextureMipmapLimit = newValue;
                //Save
                PlayerPrefs.SetInt("textureResolution", newValue);
            }
        }
    }
}