using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace MarsFPSKit.UI
{
    [System.Serializable]
    public class OptionsCategory
    {
        /// <summary>
        /// Name of the category
        /// </summary>
        public LocalizedString categoryName;

        /// <summary>
        /// Options for this category
        /// </summary>
        public Kit_OptionBase[] options;
    }

    /// <summary>
    /// This is the kit's new modular options menu!
    /// </summary>
    public class Kit_MenuOptions : Kit_Base
    {
        /// <summary>
        /// ID
        /// </summary>
        public int optionsScreenId = 9;

        /// <summary>
        /// Option categories
        /// </summary>
        public OptionsCategory[] categories;

        /// <summary>
        /// Where the categories go
        /// </summary>
        public RectTransform categoryGo;
        /// <summary>
        /// Prefab for the category selection
        /// </summary>
        public GameObject categoryPrefab;

        /// <summary>
        /// List Go
        /// </summary>
        public RectTransform optionsListGo;

        /// <summary>
        /// Slider prefab
        /// </summary>
        public GameObject optionSlider;
        /// <summary>
        /// Dropdown prefab
        /// </summary>
        public GameObject optionDropdown;
        /// <summary>
        /// Toggle prefab
        /// </summary>
        public GameObject optionToggle;
        /// <summary>
        /// Remap prefab
        /// </summary>
        public GameObject optionRemap;

        /// <summary>
        /// Asks us to select a new key and shit
        /// </summary>
        public Kit_MenuModalQuestion remapModal;

        /// <summary>
        /// Category ID
        /// </summary>
        public List<GameObject[]> optionsCategories = new List<GameObject[]>();

        /// <summary>
        /// Hover Info
        /// </summary>
        public TextMeshProUGUI hoverInfo;

        private int currentCategory;

        private bool wasSetupOnce;

        private void Start()
        {
            //Create Categories
            for (int i = 0; i < categories.Length; i++)
            {
                int id = i;
                GameObject go = Instantiate(categoryPrefab, categoryGo, false);
                //Setup text
                TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
                txt.text = categories[id].categoryName.GetLocalizedString();
                //Setup Call
                Button btn = go.GetComponentInChildren<Button>();
                btn.onClick.AddListener(delegate { SelectCategory(id); });

                //Create array for storing
                GameObject[] array = new GameObject[categories[i].options.Length];
                optionsCategories.Add(array);

                //Create Options
                for (int o = 0; o < categories[i].options.Length; o++)
                {
                    int od = o;
                    if (categories[i].options[o].GetOptionType() == OptionType.Dropdown)
                    {
                        GameObject option = Instantiate(optionDropdown, optionsListGo, false);
                        //Add
                        optionsCategories[id][od] = option;
                        TextMeshProUGUI oTxt = option.GetComponentInChildren<TextMeshProUGUI>();
                        oTxt.text = categories[i].options[o].GetDisplayName();
                        EventTrigger et = option.AddComponent<EventTrigger>();
                        EventTrigger.Entry hover = new EventTrigger.Entry();
                        hover.eventID = EventTriggerType.PointerEnter;
                        hover.callback.AddListener(delegate { Hover(id, od); });
                        et.triggers.Add(hover);
                        TMP_Dropdown dropdown = option.GetComponentInChildren<TMP_Dropdown>();
                        categories[id].options[od].OnDropdownStart(oTxt, dropdown);
                        dropdown.onValueChanged.AddListener(delegate { categories[id].options[od].OnDropdowChange(oTxt, dropdown.value); });

                        //Hide
                        option.SetActive(false);
                    }
                    else if (categories[i].options[o].GetOptionType() == OptionType.Slider)
                    {
                        GameObject option = Instantiate(optionSlider, optionsListGo, false);
                        //Add
                        optionsCategories[id][od] = option;
                        TextMeshProUGUI oTxt = option.GetComponentInChildren<TextMeshProUGUI>();
                        oTxt.text = categories[i].options[o].GetDisplayName();
                        EventTrigger et = option.AddComponent<EventTrigger>();
                        EventTrigger.Entry hover = new EventTrigger.Entry();
                        hover.eventID = EventTriggerType.PointerEnter;
                        hover.callback.AddListener(delegate { Hover(id, od); });
                        et.triggers.Add(hover);
                        Slider slider = option.GetComponentInChildren<Slider>();
                        categories[id].options[od].OnSliderStart(oTxt, slider);
                        slider.onValueChanged.AddListener(delegate { categories[id].options[od].OnSliderChange(oTxt, slider.value); });
                        //Hide
                        option.SetActive(false);
                    }
                    else if (categories[i].options[o].GetOptionType() == OptionType.Toggle)
                    {
                        GameObject option = Instantiate(optionToggle, optionsListGo, false);
                        //Add
                        optionsCategories[id][od] = option;
                        TextMeshProUGUI oTxt = option.GetComponentInChildren<TextMeshProUGUI>();
                        oTxt.text = categories[i].options[o].GetDisplayName();
                        EventTrigger et = option.AddComponent<EventTrigger>();
                        EventTrigger.Entry hover = new EventTrigger.Entry();
                        hover.eventID = EventTriggerType.PointerEnter;
                        hover.callback.AddListener(delegate { Hover(id, od); });
                        et.triggers.Add(hover);
                        Toggle toggle = option.GetComponentInChildren<Toggle>();
                        categories[id].options[od].OnToggleStart(oTxt, toggle);
                        toggle.onValueChanged.AddListener(delegate { categories[id].options[od].OnToggleChange(oTxt, toggle.isOn); });

                        //Hide
                        option.SetActive(false);
                    }
                    else if (categories[i].options[o].GetOptionType() == OptionType.Remap)
                    {
                        GameObject option = Instantiate(optionRemap, optionsListGo, false);
                        //Add
                        optionsCategories[id][od] = option;
                        Kit_OptionRemap remap = option.GetComponentInChildren<Kit_OptionRemap>();
                        remap.options = this;
                        remap.title.text = categories[i].options[o].GetDisplayName();
                        remap.onHover.AddListener(delegate { Hover(id, od); });
                        categories[id].options[od].OnRemapStart(remap);

                        remap.btn.onClick.AddListener(delegate { categories[id].options[od].OnRemapChange(remap); });
                        //Hide
                        option.SetActiveOptimized(false);
                    }
                }
            }

            //Select default one
            SelectCategory(currentCategory);
            wasSetupOnce = true;
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            RedrawLocalization();
        }

        private void RedrawLocalization()
        {
            //Create Categories
            for (int i = 0; i < categories.Length; i++)
            {
                int id = i;
                if (categoryGo.childCount > i)
                {
                    //Setup text
                    TextMeshProUGUI txt = categoryGo.GetChild(i).GetComponentInChildren<TextMeshProUGUI>();
                    txt.text = categories[id].categoryName.GetLocalizedString();

                    //Create Options
                    for (int o = 0; o < categories[i].options.Length; o++)
                    {
                        int od = o;

                        if (optionsCategories[id].Length > od)
                        {
                            //Update Text
                            GameObject option = optionsCategories[id][od];
                            if (option)
                            {
                                TextMeshProUGUI oTxt = option.GetComponentInChildren<TextMeshProUGUI>();
                                oTxt.text = categories[i].options[o].GetDisplayName();
                            }
                        }
                    }
                }
            }

            if (wasSetupOnce)
            {
                //Select default one
                SelectCategory(currentCategory);
            }
        }

        public void SelectCategory(int id)
        {
            for (int i = 0; i < optionsCategories.Count; i++)
            {
                for (int o = 0; o < optionsCategories[i].Length; o++)
                {
                    if (optionsCategories[i][o])
                        optionsCategories[i][o].SetActive(id == i);
                }
            }

            Hover(id, 0);
            currentCategory = id;
        }

        public void Hover(int cat, int id)
        {
            if (cat < categories.Length && id < categories[cat].options.Length)
            {
                if (hoverInfo)
                {
                    hoverInfo.text = categories[cat].options[id].GetHoverText();
                }
            }
        }
    }
}