using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

namespace MarsFPSKit
{
    namespace UI
    {
        public enum OptionType { Slider, Dropdown, Toggle, Remap }

        public abstract class Kit_OptionBase : ScriptableObject
        {
            public LocalizedString displayName;
            public LocalizedString hover;

            /// <summary>
            /// Returns the name that is displayed
            /// </summary>
            /// <returns></returns>
            public string GetDisplayName()
            {
                return displayName.GetLocalizedString();
            }

            /// <summary>
            /// Returns the hover text
            /// </summary>
            /// <returns></returns>
            public string GetHoverText()
            {
                return hover.GetLocalizedString();
            }

            /// <summary>
            /// What type is this setting?
            /// </summary>
            /// <returns></returns>
            public abstract OptionType GetOptionType();

            public virtual void OnToggleChange(TextMeshProUGUI txt, bool newValue)
            {

            }

            public virtual void OnToggleStart(TextMeshProUGUI txt, Toggle toggle)
            {

            }

            public virtual void OnDropdowChange(TextMeshProUGUI txt, int newValue)
            {

            }

            public virtual void OnDropdownStart(TextMeshProUGUI txt, TMP_Dropdown dropdown)
            {

            }

            public virtual void OnSliderChange(TextMeshProUGUI txt, float newValue)
            {

            }

            public virtual void OnSliderStart(TextMeshProUGUI txt, Slider slider)
            {

            }

            public virtual void OnRemapStart(Kit_OptionRemap remap)
            {

            }

            public virtual void OnRemapOpen(Kit_OptionRemap remap)
            {

            }


            public virtual void OnRemapApply(Kit_OptionRemap remap)
            {

            }

            public virtual void OnRemapChange(Kit_OptionRemap remap)
            {

            }

            public virtual void OnRemapReset(Kit_OptionRemap remap)
            {

            }
        }
    }
}