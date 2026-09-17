using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MarsFPSKit.Networking;

namespace MarsFPSKit
{
    [System.Serializable]
    public class AssistedKillData
    {
        /// <summary>
        /// ID of the bot / Photon Actor Number of the player (if not a bot)
        /// </summary>
        public uint id;
        /// <summary>
        /// Represents a bot?
        /// </summary>
        public bool bot;
    }

    [CreateAssetMenu(menuName = "MarsFPSKit/Assists/Default")]
    public class Kit_AssistManagerDefault : Kit_AssistManagerBase
    {
        /// <summary>
        /// How much xp is gained per assist?
        /// </summary>
        public int xpPerAssist = 20;

        public override void ServerOnStart()
        {

        }

        public override void ClientOnStart()
        {

        }

        public override void ServerPlayerDamaged(bool botShot, uint shotId, Kit_PlayerBehaviour damagedPlayer, float dmg)
        {
            //Assists only for team gamemodes
            if (main.currentPvPGameModeBehaviour && main.currentPvPGameModeBehaviour.isTeamGameMode)
            {
                if (damagedPlayer.damagedBy.Where(x => x.bot == botShot && x.id == shotId).Count() <= 0)
                {
                    damagedPlayer.damagedBy.Add(new AssistedKillData { bot = botShot, id = shotId });
                }
            }
        }

        public override void ServerPlayerKilled(bool botKiller, uint idKiller, Kit_PlayerBehaviour killedPlayer)
        {
            if (main.currentPvPGameModeBehaviour && main.currentPvPGameModeBehaviour.isTeamGameMode)
            {
                for (int i = 0; i < killedPlayer.damagedBy.Count; i++)
                {
                    //Check if it counts as assist
                    if (!(killedPlayer.damagedBy[i].bot == botKiller && killedPlayer.damagedBy[i].id == idKiller))
                    {
                        int killerTeam = -1;
                        int assistTeam = -2;

                        if (botKiller)
                        {
                            Kit_Bot killerBot = main.currentBotManager.GetBotWithID(idKiller);
                            if (killerBot != null)
                            {
                                killerTeam = killerBot.team;
                            }
                        }
                        else
                        {
                            Kit_Player killerPlayer = networkPlayerManager.GetPlayerById(idKiller);

                            if (killerPlayer != null)
                            {
                                killerTeam = killerPlayer.team;
                            }
                        }

                        //Assist for bot
                        if (killedPlayer.damagedBy[i].bot)
                        {
                            Kit_Bot bot = main.currentBotManager.GetBotWithID(killedPlayer.damagedBy[i].id);
                            if (bot != null)
                            {
                                assistTeam = bot.team;

                                if (assistTeam == killerTeam)
                                {
                                    bot.assists++;
                                    main.currentBotManager.ModifyBotData(bot);
                                }
                            }
                        }
                        else
                        {
                            Kit_Player player = networkPlayerManager.GetPlayerById(killedPlayer.damagedBy[i].id);

                            if (player != null)
                            {
                                assistTeam = player.team;
                            }

                            if (assistTeam == killerTeam)
                            {
                                player.assists++;
                                networkPlayerManager.ModifyPlayerData(player);

                                main.RpcGenericEvent(0, 0);
                            }
                        }
                    }
                }
            }
        }

        public override void ClientOnGenericEvent(byte eventCode, int content)
        {
            if (eventCode == 0)
            {
                if (main.gameInformation.leveling)
                {
                    main.gameInformation.leveling.AddXp(xpPerAssist);
                }

                main.pointsUI.DisplayPoints(xpPerAssist, PointType.Assist);

                if (main.gameInformation.statistics)
                {
                    //Call
                    main.gameInformation.statistics.OnAssist();
                }
            }
        }
    }
}