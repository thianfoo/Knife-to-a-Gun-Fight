using MarsFPSKit.Networking;
using Mirror;
using System.Linq;
using UnityEngine;

namespace MarsFPSKit.UI
{
    /// <summary>
    /// This script handles the victory screen. Since there is no special functions for this, it is not abstract. You can replace it with your own.
    /// </summary>
    public class Kit_VictoryScreen : Kit_BaseNetworked
    {
        [SyncVar]
        /// <summary>
        /// Winner type. 0 = Player/Bot, 1 = Team, 2 = Team with score
        /// </summary>
        public int winnerType;
        [SyncVar]
        /// <summary>
        /// Winner id (either the player/bot or the team that won)
        /// </summary>
        public uint winnerId;
        [SyncVar]
        /// <summary>
        /// If <see cref="winnerType"/> is 0, is the winner a bot?
        /// </summary>
        public bool winnerBot;
        /// <summary>
        /// Scores if a team won
        /// </summary>
        public readonly SyncList<int> winnerScores = new SyncList<int>();

        public override void OnStartClient()
        {
            //Assign to main behaviour
            main.currentVictoryScreen = this;
            //Callback
            main.VictoryScreenOpened();
            //Close Pause Menu
            main.SetPauseMenuState(false);
            //Unlock cursor
            if (MarsScreen.lockCursor)
            {
                MarsScreen.lockCursor = false;
            }
            //Disable scoreboard
            main.scoreboard.Disable();

            //Player won
            if (winnerType == 0)
            {
                if (winnerBot)
                {
                    if (main.currentBotManager)
                    {
                        Kit_Bot winner = main.currentBotManager.GetBotWithID(winnerId);
                        //Display UI
                        main.victoryScreenUI.DisplayBotWinner(winner);
                    }
                }
                else
                {
                    Kit_Player winner = networkPlayerManager.GetPlayerById(winnerId);
                    //Check if we won
                    if (winner.isLocal)
                    {
                        //We won this match!
                        DebugLog("Victory Screen: We won");
                    }
                    else
                    {
                        //Someone else won :(
                        DebugLog("Victory Screen: A different player won");
                    }
                    //Display UI
                    main.victoryScreenUI.DisplayPlayerWinner(winner);
                }
            }
            //Team won (Or draw)
            else if (winnerType == 1)
            {
                //Check which team won
                if (winnerId == 999)
                {
                    //Draw
                    DebugLog("Victory Screen: Draw");
                }
                else
                {
                    //Team x won
                    DebugLog("Victory Screen: Team " + winnerId + " won");
                }
                //Check if the data inlcudes scores
                if (winnerScores.Count > 0)
                {
                    //Display UI
                    main.victoryScreenUI.DisplayTeamWinnerWithScores(winnerId, winnerScores.ToArray());
                }
                else
                {
                    //Display UI
                    main.victoryScreenUI.DisplayTeamWinner(winnerId);
                }
            }
        }

        void OnDestroy()
        {
            //Hide UI
            main.victoryScreenUI.CloseUI();
            //Enable scoreboard
            main.scoreboard.Enable();
        }
    }
}
