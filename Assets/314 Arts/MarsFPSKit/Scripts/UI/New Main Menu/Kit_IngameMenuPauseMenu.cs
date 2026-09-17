using MarsFPSKit.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace MarsFPSKit.UI
{
    public class Kit_IngameMenuPauseMenu : Kit_Base
    {
        /// <summary>
        /// ID for the pause menu
        /// </summary>
        public int pauseMenuId = 1;
        /// <summary>
        /// Button that takes us to the loadout menu
        /// </summary>
        public GameObject loadoutButton;
        /// <summary>
        /// Spawn  Button
        /// </summary>
        public TextMeshProUGUI spawnButtonText;
        /// <summary>
        /// Because this button controls both, changing teams and commiting suicide, we need to adjust its text
        /// </summary>
        public TextMeshProUGUI changeTeamButtonText;
        /// <summary>
        /// The change team button
        /// </summary>
        public GameObject changeTeamButton;
        /// <summary>
        /// Button that lets us vote
        /// </summary>
        public GameObject voteButton;
        /// <summary>
        /// Transform where the button goes
        /// </summary>
        public RectTransform pluginButtonGo;
        /// <summary>
        /// Prefab for inejcting a button into the pause menu
        /// </summary>
        public GameObject pluginButtonPrefab;

        public LocalizedString suicide;
        public LocalizedString changeTeam;
        public LocalizedString resume;
        public LocalizedString spawn;
        public LocalizedString close;

        private void Start()
        {
            //Is loadout supported?
            if (main.currentPvPGameModeBehaviour && main.currentPvPGameModeBehaviour.LoadoutMenuSupported())
            {
                loadoutButton.SetActive(true);
            }
            else
            {
                loadoutButton.SetActive(false);
            }
        }

        private void Update()
        {
            #region Team - Suicide Button
            if (main.currentPvEGameModeBehaviour)
            {
                changeTeamButton.gameObject.SetActiveOptimized(false);
            }
            else if (main.myPlayer)
            {
                changeTeamButtonText.text = suicide.GetLocalizedString();
                changeTeamButton.gameObject.SetActiveOptimized(true);
            }
            else if (main.currentPvPGameModeBehaviour)
            {
                changeTeamButtonText.text = changeTeam.GetLocalizedString();
                changeTeamButton.gameObject.SetActiveOptimized(true);
            }
            else
            {
                changeTeamButton.gameObject.SetActiveOptimized(false);
            }
            #endregion

            #region Spawn/Resume Button
            if (main.myPlayer)
            {
                spawnButtonText.text = resume.GetLocalizedString();
            }
            else
            {
                if (main.currentPvPGameModeBehaviour && main.currentPvPGameModeBehaviour.CanSpawn(networkPlayerManager.GetLocalPlayer()))
                {
                    spawnButtonText.text = spawn.GetLocalizedString();
                }
                else
                {
                    spawnButtonText.text = close.GetLocalizedString();
                }
            }
            #endregion

            #region Vote Button
            //Voting only in pvp game modes
            voteButton.SetActiveOptimized(main.currentPvPGameModeBehaviour);
            #endregion
        }
    }
}