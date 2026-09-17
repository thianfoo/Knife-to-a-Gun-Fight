using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace MarsFPSKit
{
    namespace UI
    {
        [CreateAssetMenu(menuName = "MarsFPSKit/Options/Game/Language")]
        public class Kit_OptionsLanguage : Kit_OptionBase
        {
            [System.Serializable]
            public class LanguageOption
            {
                public string displayName;
                public Locale locale;
            }

            public LanguageOption[] languages;

            public override OptionType GetOptionType()
            {
                return OptionType.Dropdown;
            }

            public override void OnDropdownStart(TextMeshProUGUI txt, TMP_Dropdown dropdown)
            {
                //Load
                int selectedLanguage = PlayerPrefs.GetInt("language", 0);
                //Clamp
                selectedLanguage = Mathf.Clamp(selectedLanguage, 0, languages.Length - 1);
                //Clear
                dropdown.ClearOptions();
                List<string> options = new List<string>();
                for (int i = 0; i < languages.Length; i++)
                {
                    options.Add(languages[i].displayName);
                }
                //Add
                dropdown.AddOptions(options);
                //Set default value
                dropdown.value = selectedLanguage;
                //Use that value
                OnDropdowChange(txt, selectedLanguage);
            }

            public override void OnDropdowChange(TextMeshProUGUI txt, int newValue)
            {
                //Set
                LocalizationSettings.SelectedLocale = languages[newValue].locale;
                //Save
                PlayerPrefs.SetInt("language", newValue);
            }
        }
    }
}