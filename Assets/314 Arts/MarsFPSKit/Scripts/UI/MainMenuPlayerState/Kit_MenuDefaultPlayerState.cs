using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarsFPSKit
{
    namespace UI
    {
        public class Kit_MenuDefaultPlayerState : Kit_MenuPlayerStateBase
        {
            /// <summary>
            /// This displays the username
            /// </summary>
            public TextMeshProUGUI username;

            /// <summary>
            /// Displays the current rank
            /// </summary>
            public Image rank;

            /// <summary>
            /// This displays % in a bar
            /// </summary>
            public Image levelUpFill;

            /// <summary>
            /// This displays the % of the levelup
            /// </summary>
            public TextMeshProUGUI levelUpPercentage;

            public override void Initialize(Kit_MenuManager main)
            {
                if (username) username.text = Kit_GameSettings.userName;

                //Check if we have leveling
                if (game.leveling)
                {
                    //Get info
                    if (rank) rank.sprite = game.leveling.levelUpIcons[game.leveling.GetLevel() - 1];
                    if (levelUpFill) levelUpFill.fillAmount = game.leveling.GetPercentageToNextLevel();
                    if (levelUpPercentage) levelUpPercentage.text = (game.leveling.GetPercentageToNextLevel() * 100f).ToString("F0") + "%";

                    //Enable level related stuff
                    if (rank) rank.enabled = true;
                    if (levelUpFill) levelUpFill.enabled = true;
                    if (levelUpPercentage) levelUpPercentage.enabled = true;
                }
                else
                {
                    //Else, disable level related stuff
                    if (rank) rank.enabled = false;
                    if (levelUpFill) levelUpFill.enabled = false;
                    if (levelUpPercentage) levelUpPercentage.enabled = false;
                }
            }
        }
    }
}
