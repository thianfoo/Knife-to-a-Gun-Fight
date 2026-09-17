using MarsFPSKit.Networking;
using System;
using System.Collections;

namespace MarsFPSKit
{
    public abstract class Kit_GameModeNetworkDataBase : Kit_BaseNetworked
    {
        public Kit_PvP_GameModeBase gameModePvp;
        public Kit_PvE_GameModeBase gameModePve;

        public override void OnStartServer()
        {
            StartCoroutine(ServerRoutine());
        }


        IEnumerator ServerRoutine()
        {
            while (!main) yield return null;

            main.currentGameModeRuntimeData = this;
        }

        public override void OnStartClient()
        {
            StartCoroutine(ClientRoutine());
        }

        IEnumerator ClientRoutine()
        {
            while (!main) yield return null;

            main.currentGameModeRuntimeData = this;

            //Make sure we have the correct game mode set before we call setup

            if (gameModePvp)
            {
                networkGameInformation.gameMode = Array.IndexOf(main.gameInformation.allPvpGameModes, gameModePvp);
            }

            if (gameModePve)
            {
                if (networkGameInformation.gameModeType == 0)
                {
                    networkGameInformation.gameMode = Array.IndexOf(main.gameInformation.allSingleplayerGameModes, gameModePve);
                }
                else
                {
                    networkGameInformation.gameMode = Array.IndexOf(main.gameInformation.allCoopGameModes, gameModePve);
                }
            }

            main.OnClientDataReadyToSetup();
        }
    }
}