using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace MarsFPSKit.UI
{
    public class Kit_IngameMenuTeamSelection : Kit_Base
    {
        /// <summary>
        /// Id in the team selection
        /// </summary>
        public int teamSelectionId;
        /// <summary>
        /// Where the team selection goes
        /// </summary>
        public RectTransform teamGo;
        /// <summary>
        /// Prefab for team selection
        /// </summary>
        public GameObject teamPrefab;

        /// <summary>
        /// Action after team selection
        /// </summary>
        public AfterTeamSelection afterSelection;

        public LocalizedString spectate;

        public void Setup()
        {
            for (sbyte i = 0; i < Mathf.Clamp(main.gameInformation.allPvpTeams.Length, 0, main.currentPvPGameModeBehaviour.maximumAmountOfTeams); i++)
            {
                sbyte id = i;
                GameObject go = Instantiate(teamPrefab, teamGo, false);
                Button btn = go.GetComponentInChildren<Button>();
                TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();

                btn.onClick.AddListener(delegate { main.JoinTeam(id); });
                txt.text = main.gameInformation.allPvpTeams[id].teamName.GetLocalizedString();

                //Move to right pos
                go.transform.SetSiblingIndex(id);
            }

            if (main.spectatorManager && main.spectatorManager.IsSpectatingEnabled() && main.currentPvPGameModeBehaviour.SpectatingEnabled())
            {
                GameObject go = Instantiate(teamPrefab, teamGo, false);
                Button btn = go.GetComponentInChildren<Button>();
                TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();

                btn.onClick.AddListener(delegate { main.spectatorManager.BeginSpectating(true); });
                txt.text = spectate.GetLocalizedString();

                //Move to right pos
                go.transform.SetSiblingIndex(Mathf.Clamp(main.gameInformation.allPvpTeams.Length, 0, main.currentPvPGameModeBehaviour.maximumAmountOfTeams));
            }
        }

        public void Open()
        {
            main.SwitchMenu(teamSelectionId);
        }

    }
}