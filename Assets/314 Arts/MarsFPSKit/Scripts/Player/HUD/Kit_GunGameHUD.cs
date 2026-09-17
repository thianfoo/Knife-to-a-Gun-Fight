using MarsFPSKit.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarsFPSKit
{
    /// <summary>
    /// This is used for <see cref="Kit_PvP_GMB_GunGame"/>
    /// </summary>
    public class Kit_GunGameHUD : Kit_GameModeHUDBase
    {
        public TextMeshProUGUI timer;

        public TextMeshProUGUI nextWeaponDisplay;

        private int roundedRestSeconds;
        private int displaySeconds;
        private int displayMinutes;

        public override void HUDUpdate()
        {
            if (main.currentPvPGameModeBehaviour.AreEnoughPlayersThere() || main.hasGameModeStarted)
            {
                roundedRestSeconds = Mathf.CeilToInt(main.timer);
                displaySeconds = roundedRestSeconds % 60; //Get seconds
                displayMinutes = roundedRestSeconds / 60; //Get minutes
                                                          //Update text
                timer.text = string.Format("{0:00} : {1:00}", displayMinutes, displaySeconds);
                timer.enabled = true;

                //Only display next weapon if the player is spawned
                if (main.myPlayer)
                {
                    Kit_PvP_GMB_GunGame gameMode = main.currentPvPGameModeBehaviour as Kit_PvP_GMB_GunGame;
                    //Get Game Mode data
                    Kit_PvP_GMB_GunGameNetworkData ggrd = main.currentGameModeRuntimeData as Kit_PvP_GMB_GunGameNetworkData;
                    if (ggrd != null)
                    {
                        int currentGun = 0;

                        if (ggrd.currentGun.ContainsKey(networkPlayerManager.myId))
                        {
                            currentGun = ggrd.currentGun[networkPlayerManager.myId];
                        }

                        if (currentGun + 1 < gameMode.weaponOrders[ggrd.currentGunOrder].weapons.Length)
                        {
                            nextWeaponDisplay.text = "Next Weapon: " + main.gameInformation.allWeapons[gameMode.weaponOrders[ggrd.currentGunOrder].weapons[currentGun + 1]].weaponName.GetLocalizedString();
                        }
                        else
                        {
                            nextWeaponDisplay.text = "Final level reached";
                        }
                    }
                    nextWeaponDisplay.enabled = true;
                }
                else
                {
                    nextWeaponDisplay.enabled = false;
                }
            }
            else
            {
                timer.enabled = false;
                //Disable next weapon
                nextWeaponDisplay.enabled = false;
            }
        }
    }
}
