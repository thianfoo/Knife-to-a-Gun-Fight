using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace MarsFPSKit.UI
{
    /// <summary>
    /// Displays the player prefs based statistics in the menu
    /// </summary>
    public class Kit_MenuStatisticsForPlayerPrefs : Kit_Base
    {
        /// <summary>
        /// Menu Manager reference
        /// </summary>
        public Kit_MenuManager menuManager;

        /// <summary>
        /// Kills
        /// </summary>
        public TextMeshProUGUI kills;
        /// <summary>
        /// Assists
        /// </summary>
        public TextMeshProUGUI assists;
        /// <summary>
        /// Deaths
        /// </summary>
        public TextMeshProUGUI deaths;
        /// <summary>
        /// K/D
        /// </summary>
        public TextMeshProUGUI kd;

        public LocalizedString localizationKills;
        public LocalizedString localizationAssists;
        public LocalizedString localizationDeaths;
        public LocalizedString localizationKd;

        private void Awake()
        {
            menuManager.onLogin.AddListener(delegate { RedrawStatistics(); });
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
            RedrawStatistics();
        }

        public void RedrawStatistics()
        {
            if (game.statistics && game.statistics.GetType() == typeof(Kit_StatisticsPlayerPrefs))
            {
                Kit_StatisticsPlayerPrefs kspp = game.statistics as Kit_StatisticsPlayerPrefs;

                //Just set texts
                kills.text = string.Format(localizationKills.GetLocalizedString(), kspp.kills);
                assists.text = string.Format(localizationAssists.GetLocalizedString(), kspp.assists);
                deaths.text = string.Format(localizationDeaths.GetLocalizedString(), kspp.deaths);
                if (kspp.deaths > 0) string.Format(localizationKd.GetLocalizedString(), ((float)kspp.kills / kspp.deaths).ToString("F1"));
                else kd.text = string.Format(localizationKd.GetLocalizedString(), kspp.kills);
            }
        }
    }
}