using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using MarsFPSKit.Spectating;
using Mirror;
using MarsFPSKit.Networking;
using System.Xml.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MarsFPSKit
{
    [CreateAssetMenu(menuName = ("MarsFPSKit/Gamemodes/Domination Logic"))]
    public class Kit_PvP_GMB_Domination : Kit_PvP_GameModeBase
    {
        [Header("Domination Settings")]
        /// <summary>
        /// How many points does a team need to win?
        /// </summary>
        public int pointLimit = 200;

        /// <summary>
        /// How many points does a team get per owned flag per tick?
        /// </summary>
        public int pointsPerOwnedFlag = 1;

        /// <summary>
        /// How many points does a player get for capturing a flag?
        /// </summary>
        public int xpForCapturingFlag = 25;

        /// <summary>
        /// How long is a tick (time between addition of points)? It is recommended to use something larger than .256 seconds because of network in accuracies in photon
        /// </summary>
        public float tickTime = 5f;

        /// <summary>
        /// Speed at which flag capturing reaches 100% (base)
        /// </summary>
        public float flagCaptureSpeed = 5;

        /// <summary>
        /// Multiplier at which the flag capture speed is multiplied per player capturing
        /// </summary>
        public float flagCaptureSpeedPlayerCountMultiplier = 2f;

        /// <summary>
        /// The prefab used for the flags
        /// </summary>
        public GameObject flagPrefab;

        /// <summary>
        /// Material used when flag is neutral
        /// </summary>
        public Material flagMaterialNeutral;
        /// <summary>
        /// Material used by team one
        /// </summary>
        public Material[] flagMaterialTeams;

        /// <summary>
        /// Flag is owned by no one and no one is capturing it
        /// </summary>
        [Header("HUD Colors")]
        public Color hudColorNeutral = Color.white;
        /// <summary>
        /// Both teams are trying to capture this flag
        /// </summary>
        public Color hudColorFlagFightedFor = Color.yellow;

        [Tooltip("The maximum amount of difference the teams can have in player count")]
        /// <summary>
        /// The maximum amount of difference the teams can have in player count
        /// </summary>
        public int maxTeamDifference = 2;

        /// <summary>
        /// How many seconds need to be left in order to be able to start a vote?
        /// </summary>
        public float votingThreshold = 30f;

        [Header("Times")]
        /// <summary>
        /// How many seconds until we can start playing? This is the first countdown during which players cannot move or do anything other than spawn or chat.
        /// </summary>
        public float preGameTime = 20f;

        /// <summary>
        /// How many seconds until the map/gamemode voting menu is opened
        /// </summary>
        public float endGameTime = 10f;

        /// <summary>
        /// How many seconds do we have to vote on the next map and game mode?
        /// </summary>
        public float mapVotingTime = 20f;

        /// <summary>
        /// Spawn layer used for team one during countdown
        /// </summary>
        [Tooltip("Spawn layer used for team one during countdown")]
        [Header("Spawns")]
        public int[] teamInitialSpawnLayer;
        /// <summary>
        /// Spawn layer used for team two during gameplay
        /// </summary>
        [Tooltip("Spawn layer used for team one during gameplay")]
        public int[] teamGameplaySpawnLayer;

        public override Spectateable GetSpectateable()
        {
            if (main.assignedTeamID >= 0) return Spectateable.Friendlies;

            return Spectateable.All;
        }

        public override bool CanJoinTeam(NetworkConnectionToClient player, int team)
        {
            int amountOfPlayers = networkGameInformation.playerLimit;

            //Check if the team has met its limits
            if (team == 0 && playersInTeamOne > amountOfPlayers / 2)
            {
                return false;
            }
            else if (team == 1 && playersInTeamTwo > amountOfPlayers / 2)
            {
                return false;
            }

            //Check if the difference is too big
            if (team == 0)
            {
                if (playersInTeamOne - playersInTeamTwo > maxTeamDifference) return false;
            }
            else if (team == 1)
            {
                if (playersInTeamTwo - playersInTeamOne > maxTeamDifference) return false;
            }

            //If none of the excluding factors were met, return true
            return true;
        }

        public override void GamemodeSetupServer()
        {
            //Create network data
            GameObject nData = Instantiate(networkData, Vector3.zero, Quaternion.identity);
            main.currentGameModeRuntimeData = nData.GetComponent<Kit_GameModeNetworkDataBase>();
            NetworkServer.Spawn(nData);

            Kit_PvP_GMB_DominationNetworkData dData = nData.GetComponent<Kit_PvP_GMB_DominationNetworkData>();

            for (int i = 0; i < Mathf.Clamp(main.gameInformation.allPvpTeams.Length, 0, maximumAmountOfTeams); i++)
            {
                dData.teamPoints.Add(0);
            }

            //Get all spawns
            Kit_PlayerSpawn[] allSpawns = FindObjectsByType<Kit_PlayerSpawn>(FindObjectsSortMode.None);
            //Are there any spawns at all?
            if (allSpawns.Length <= 0) throw new Exception("This scene has no spawns.");

            //Get all flags
            //In a determinstic order too (thats what the orderby is for)
            Kit_Domination_Flag[] allFlags = FindObjectsByType<Kit_Domination_Flag>(FindObjectsSortMode.None).OrderBy(m => m.transform.GetSiblingIndex()).ToArray();

            //Instantiate all flags
            for (int i = 0; i < allFlags.Length; i++)
            {
                GameObject go = Instantiate(flagPrefab, allFlags[i].transform.position, allFlags[i].transform.rotation);
                Kit_Domination_FlagRuntime dfd = go.GetComponent<Kit_Domination_FlagRuntime>();
                dfd.flagId = i;
                go.name = flagPrefab.name + "(Clone) - " + i;
                //Spawn on network
                NetworkServer.Spawn(go);
            }

            //Filter all spawns that are appropriate for this game mode
            List<Kit_PlayerSpawn> filteredSpawns = new List<Kit_PlayerSpawn>();
            //Highest spawn index
            int highestIndex = 0;

            for (int i = 0; i < allSpawns.Length; i++)
            {
                int id = i;
                if (allSpawns[id].pvpGameModes.Contains(this))
                {
                    //Add it to the list
                    filteredSpawns.Add(allSpawns[id]);
                    //Set highest index
                    if (allSpawns[id].spawnGroupID > highestIndex) highestIndex = allSpawns[id].spawnGroupID;
                }
            }

            for (int o = 0; o < dData.flags.Count; o++)
            {
                dData.flags[o].navPointsForFlag = allFlags[o].botNavPoints;
                dData.flags[o].navPointsAvailable = allFlags[o].botNavPoints.ToList();
                dData.flags[o].spawnForFlag = allFlags[o].spawns;
            }

            Kit_BotNavPoint[] navPoints = FindObjectsByType<Kit_BotNavPoint>(FindObjectsSortMode.None);

            //Setup spawn list
            main.internalSpawns = new List<InternalSpawns>();
            for (int i = 0; i < (highestIndex + 1); i++)
            {
                main.internalSpawns.Add(null);
            }
            //Setup spawn lists
            for (int i = 0; i < main.internalSpawns.Count; i++)
            {
                int id = i;
                main.internalSpawns[id] = new InternalSpawns();
                main.internalSpawns[id].spawns = new List<Kit_PlayerSpawn>();
                for (int o = 0; o < filteredSpawns.Count; o++)
                {
                    int od = o;
                    if (filteredSpawns[od].spawnGroupID == id)
                    {
                        main.internalSpawns[id].spawns.Add(filteredSpawns[od]);
                    }
                }
            }

            //Set stage and timer
            main.gameModeStage = 0;
            main.timer = preGameTime;
        }

        public override void GamemodeSetupClient()
        {
            //Kit_PvP_GMB_DominationNetworkData dData = main.currentGameModeRuntimeData.GetComponent<Kit_PvP_GMB_DominationNetworkData>();
        }

        public override void GameModeUpdate()
        {
            if (main.currentGameModeRuntimeData != null && main.currentGameModeRuntimeData.GetType() == typeof(Kit_PvP_GMB_DominationNetworkData))
            {
                Kit_PvP_GMB_DominationNetworkData drd = main.currentGameModeRuntimeData as Kit_PvP_GMB_DominationNetworkData;
                if (drd != null)
                {
                    //Update flags
                    UpdateFlagProgression(drd);
                    //Update points every tick
                    if (NetworkTime.time > drd.lastTick)
                    {
                        drd.lastTick = NetworkTime.time + tickTime;
                        OneTick(drd);
                        if (main.gameModeStage < 2)
                        {
                            CheckForWinner();
                        }
                    }

                    //Update Flag materials
                    UpdateFlagMaterial(drd);

                    //Smooth progress
                    for (int i = 0; i < drd.flags.Count; i++)
                    {
                        drd.flags[i].smoothedCaptureProgress = Mathf.Lerp(drd.flags[i].smoothedCaptureProgress, drd.flags[i].captureProgress, Time.deltaTime * 10f);
                    }
                }
            }
        }

        public override void GameModeUpdateOthers()
        {
            if (main.currentGameModeRuntimeData != null && main.currentGameModeRuntimeData.GetType() == typeof(Kit_PvP_GMB_DominationNetworkData))
            {
                Kit_PvP_GMB_DominationNetworkData drd = main.currentGameModeRuntimeData as Kit_PvP_GMB_DominationNetworkData;
                if (drd != null)
                {
                    //Update Flag materials
                    UpdateFlagMaterial(drd);

                    //Smooth progress
                    for (int i = 0; i < drd.flags.Count; i++)
                    {
                        drd.flags[i].smoothedCaptureProgress = Mathf.Lerp(drd.flags[i].smoothedCaptureProgress, drd.flags[i].captureProgress, Time.deltaTime * 10f);
                    }
                }
            }
        }

        void OneTick(Kit_PvP_GMB_DominationNetworkData drd)
        {
            //Check through all flags
            for (int i = 0; i < drd.flags.Count; i++)
            {
                if (drd.flags[i].currentOwner > 0)
                {
                    drd.teamPoints[drd.flags[i].currentOwner - 1] += pointsPerOwnedFlag;
                }
            }

            for (int i = 0; i < drd.teamPoints.Count; i++)
            {
                //Clamp
                drd.teamPoints[i] = Mathf.Clamp(drd.teamPoints[i], 0, pointLimit);
            }
        }

        void UpdateFlagProgression(Kit_PvP_GMB_DominationNetworkData drd)
        {
            for (int i = 0; i < drd.flags.Count; i++)
            {
                if (drd.flags[i].currentState == 0)
                {
                    drd.flags[i].captureProgress = 0f;
                }
                else if (drd.flags[i].currentState >= 1)
                {
                    if (drd.flags[i].currentOwner != drd.flags[i].currentState)
                    {
                        //Calculate multiplier
                        float playerMultiplier = 1;
                        for (int o = 0; o < drd.flags[i].playersCapturingFlag; o++)
                        {
                            //Only if more than one player
                            if (o > 0)
                                playerMultiplier *= flagCaptureSpeedPlayerCountMultiplier;
                        }
                        drd.flags[i].captureProgress += Time.deltaTime * flagCaptureSpeed * playerMultiplier;
                        if (drd.flags[i].captureProgress >= 100f)
                        {
                            //Set flag owned by that team
                            drd.flags[i].currentOwner = drd.flags[i].currentState;
                            drd.flags[i].captureProgress = 0f;
                            //Inform bots
                            if (main.currentBotManager)
                            {
                                for (int o = 0; o < main.currentBotManager.bots.Count; o++)
                                {
                                    int t = o;
                                    if (main.currentBotManager.IsBotAlive(main.currentBotManager.bots[t]))
                                    {
                                        Kit_PlayerBehaviour pb = main.currentBotManager.GetAliveBot(main.currentBotManager.bots[t]);
                                        int p = i;
                                        (pb.botControls as Kit_PlayerDominationBotControl).FlagCaptured(pb, drd.flags[i], 0);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void UpdateFlagMaterial(Kit_PvP_GMB_DominationNetworkData drd)
        {
            for (int i = 0; i < drd.flags.Count; i++)
            {
                drd.flags[i].UpdateFlag(drd.flags[i].currentOwner, this);
            }
        }

        /// <summary>
        /// Called when the player state in a flag has changed
        /// </summary>
        /// <param name="main"></param>
        /// <param name="flag"></param>
        public void FlagStateChanged(Kit_Domination_FlagRuntime flag)
        {
            if (main.currentGameModeRuntimeData != null && main.currentGameModeRuntimeData.GetType() == typeof(Kit_PvP_GMB_DominationNetworkData))
            {
                Kit_PvP_GMB_DominationNetworkData drd = main.currentGameModeRuntimeData as Kit_PvP_GMB_DominationNetworkData;
                //Find id
                int id = 0;
                for (int i = 0; i < drd.flags.Count; i++)
                {
                    if (drd.flags[i] == flag)
                    {
                        id = i;
                        break;
                    }
                }

                if (flag.playersInTrigger.Count > 0)
                {
                    bool[] teamCapping = new bool[Mathf.Clamp(main.gameInformation.allPvpTeams.Length, 0, maximumAmountOfTeams)];
                    for (int i = 0; i < flag.playersInTrigger.Count; i++)
                    {
                        teamCapping[flag.playersInTrigger[i].myTeam] = true;
                    }

                    bool oneTeamCapping = false;
                    bool multipleTeamsCapping = false;

                    for (int i = 0; i < teamCapping.Length; i++)
                    {
                        if (teamCapping[i])
                        {
                            if (!oneTeamCapping)
                            {
                                oneTeamCapping = true;
                            }
                            else
                            {
                                multipleTeamsCapping = true;
                                break;
                            }
                        }
                    }

                    if (multipleTeamsCapping)
                    {
                        drd.flags[id].currentState = -1;
                        drd.flags[id].playersCapturingFlag = flag.playersInTrigger.Count;
                    }
                    else
                    {
                        int curTeamCapping = -1;

                        for (int i = 0; i < teamCapping.Length; i++)
                        {
                            int teamId = i;

                            if (teamCapping[i])
                            {
                                curTeamCapping = teamId;
                                break;
                            }
                        }

                        if (curTeamCapping == -1)
                        {
                            drd.flags[id].currentState = 0;
                            drd.flags[id].playersCapturingFlag = 0;
                        }
                        else
                        {
                            drd.flags[id].currentState = 1 + curTeamCapping;
                            drd.flags[id].playersCapturingFlag = flag.playersInTrigger.Count;
                        }
                    }
                }
                else
                {
                    drd.flags[id].currentState = 0;
                    drd.flags[id].playersCapturingFlag = 0;
                }
            }
        }

        /// <summary>
        /// Checks all players in <see cref="PhotonNetwork.PlayerList"/> if they reached the kill limit, if the game is not over already
        /// </summary>
        void CheckForWinner()
        {
            //Check if someone can still win
            if (main.gameModeStage < 2 && AreEnoughPlayersThere())
            {
                Kit_PvP_GMB_DominationNetworkData drd = main.currentGameModeRuntimeData as Kit_PvP_GMB_DominationNetworkData;

                for (int i = 0; i < drd.teamPoints.Count; i++)
                {
                    if (drd.teamPoints[i] >= pointLimit)
                    {
                        //End Game
                        main.EndGame((uint)i, drd.teamPoints.ToArray());
                        //Set game stage
                        main.timer = endGameTime;
                        main.gameModeStage = 2;
                        break;
                    }
                }
            }
        }

        int players
        {
            get
            {
                return networkPlayerManager.players.Count;
            }
        }

        /// <summary>
        /// How many players are in team one?
        /// </summary>
        int playersInTeamOne
        {
            get
            {
                int toReturn = 0;

                //Loop through all players
                for (int i = 0; i < networkPlayerManager.players.Count; i++)
                {
                    //Check if he is in team one
                    if (networkPlayerManager.players[i].team == 0) toReturn++;
                }

                //Return
                return toReturn;
            }
        }

        /// <summary>
        /// How many players are in team two?
        /// </summary>
        int playersInTeamTwo
        {
            get
            {
                int toReturn = 0;

                //Loop through all players
                for (int i = 0; i < networkPlayerManager.players.Count; i++)
                {
                    //Check if he is in team one
                    if (networkPlayerManager.players[i].team == 1) toReturn++;
                }

                //Return
                return toReturn;
            }
        }

        public override Transform GetSpawn(Kit_Player player)
        {
            Transform spawnToReturn = null;

            //Start spawns
            if (main.gameModeStage == 0)
            {
                int tries = 0;
                while (tries < 10 && !spawnToReturn)
                {
                    int team = player.team;
                    int layer = teamInitialSpawnLayer[Mathf.Clamp(team, 0, teamInitialSpawnLayer.Length - 1)];
                    Transform spawnToTest = main.internalSpawns[layer].spawns[UnityEngine.Random.Range(0, main.internalSpawns[layer].spawns.Count)].transform;
                    //Test the spawn
                    if (spawnToTest)
                    {
                        if (spawnSystemToUse.CheckSpawnPosition(spawnToTest, player))
                        {
                            //Assign spawn
                            spawnToReturn = spawnToTest;
                            //Break the while loop
                            break;
                        }
                    }
                    tries++;
                }
            }
            else
            {
                Kit_PvP_GMB_DominationNetworkData drd = main.currentGameModeRuntimeData as Kit_PvP_GMB_DominationNetworkData;

                int tries = 0;
                while (tries < 10 && !spawnToReturn)
                {
                    for (int i = 0; i < drd.flags.Count; i++)
                    {
                        if (drd.flags[i].currentOwner == (player.team + 1))
                        {
                            Transform spawnToTest = drd.flags[i].spawnForFlag[UnityEngine.Random.Range(0, drd.flags[i].spawnForFlag.Length)].transform;
                            //Test the spawn
                            if (spawnToTest)
                            {
                                if (spawnSystemToUse.CheckSpawnPosition(spawnToTest, player))
                                {
                                    //Assign spawn
                                    spawnToReturn = spawnToTest;
                                    //Break the while loop
                                    break;
                                }
                            }
                            tries++;
                        }
                    }
                    tries++;
                }
                //Use backup spawns if there is still nothing
                if (!spawnToReturn)
                {
                    //Reset tries
                    tries = 0;
                    while (tries < 10 && !spawnToReturn)
                    {
                        int team = player.team;
                        int layer = teamGameplaySpawnLayer[Mathf.Clamp(team, 0, teamGameplaySpawnLayer.Length - 1)];
                        //Backup spawns = spawns[0]
                        Transform spawnToTest = main.internalSpawns[layer].spawns[UnityEngine.Random.Range(0, main.internalSpawns[layer].spawns.Count)].transform;
                        //Test the spawn
                        if (spawnToTest)
                        {
                            if (spawnSystemToUse.CheckSpawnPosition(spawnToTest, player))
                            {
                                //Assign spawn
                                spawnToReturn = spawnToTest;
                                //Break the while loop
                                break;
                            }
                        }
                        tries++;
                    }
                }
            }

            return spawnToReturn;
        }
        public override Transform GetSpawn(Kit_Bot bot)
        {
            Transform spawnToReturn = null;

            //Start spawns
            if (main.gameModeStage == 0)
            {
                int tries = 0;
                while (tries < 10 && !spawnToReturn)
                {
                    int team = bot.team;
                    int layer = teamInitialSpawnLayer[Mathf.Clamp(team, 0, teamInitialSpawnLayer.Length - 1)];
                    Transform spawnToTest = main.internalSpawns[layer].spawns[UnityEngine.Random.Range(0, main.internalSpawns[layer].spawns.Count)].transform;
                    //Test the spawn
                    if (spawnToTest)
                    {
                        if (spawnSystemToUse.CheckSpawnPosition(spawnToTest, bot))
                        {
                            //Assign spawn
                            spawnToReturn = spawnToTest;
                            //Break the while loop
                            break;
                        }
                    }
                    tries++;
                }
            }
            else
            {
                Kit_PvP_GMB_DominationNetworkData drd = main.currentGameModeRuntimeData as Kit_PvP_GMB_DominationNetworkData;

                int tries = 0;
                while (tries < 10 && !spawnToReturn)
                {
                    for (int i = 0; i < drd.flags.Count; i++)
                    {
                        if (drd.flags[i].currentOwner == (bot.team + 1))
                        {
                            Transform spawnToTest = drd.flags[i].spawnForFlag[UnityEngine.Random.Range(0, drd.flags[i].spawnForFlag.Length)].transform;
                            //Test the spawn
                            if (spawnToTest)
                            {
                                if (spawnSystemToUse.CheckSpawnPosition(spawnToTest, bot))
                                {
                                    //Assign spawn
                                    spawnToReturn = spawnToTest;
                                    //Break the while loop
                                    break;
                                }
                            }
                            tries++;
                        }
                    }
                    tries++;
                }
                //Use backup spawns if there is still nothing
                if (!spawnToReturn)
                {
                    //Reset tries
                    tries = 0;
                    while (tries < 10 && !spawnToReturn)
                    {
                        int layer = teamGameplaySpawnLayer[Mathf.Clamp(bot.team, 0, teamGameplaySpawnLayer.Length - 1)];
                        //Backup spawns = spawns[0]
                        Transform spawnToTest = main.internalSpawns[layer].spawns[UnityEngine.Random.Range(0, main.internalSpawns[layer].spawns.Count)].transform;
                        //Test the spawn
                        if (spawnToTest)
                        {
                            if (spawnSystemToUse.CheckSpawnPosition(spawnToTest, bot))
                            {
                                //Assign spawn
                                spawnToReturn = spawnToTest;
                                //Break the while loop
                                break;
                            }
                        }
                        tries++;
                    }
                }
            }

            return spawnToReturn;
        }

        public override void PlayerDied(bool botKiller, uint killer, bool botKilled, uint killed)
        {
            Kit_PvP_GMB_DominationNetworkData drd = main.currentGameModeRuntimeData as Kit_PvP_GMB_DominationNetworkData;
            //Update flags
            for (int i = 0; i < drd.flags.Count; i++)
            {
                drd.flags[i].PlayerDied();
            }
        }

        public override void TimeRunOut()
        {
            Kit_PvP_GMB_DominationNetworkData drd = main.currentGameModeRuntimeData as Kit_PvP_GMB_DominationNetworkData;

            //Check stage
            if (main.gameModeStage == 0)
            {
                if (Kit_GameSettings.currentNetworkingMode == KitNetworkingMode.Traditional)
                {
                    //Pre game time to main game
                    main.timer = main.currentPvPGameModeBehaviour.traditionalDurations[Kit_GameSettings.gameLength];
                }
                else if (Kit_GameSettings.currentNetworkingMode == KitNetworkingMode.Lobby)
                {
                    //Pre game time to main game
                    main.timer = main.currentPvPGameModeBehaviour.lobbyGameDuration;
                }
                main.gameModeStage = 1;
            }
            //Time run out, determine winner
            else if (main.gameModeStage == 1)
            {
                main.timer = endGameTime;
                main.gameModeStage = 2;

                //Get most points
                int mostPoints = drd.teamPoints.Max();

                uint teamWon = 1000;

                for (int i = 0; i < drd.teamPoints.Count; i++)
                {
                    if (drd.teamPoints[i] == mostPoints)
                    {
                        //No other team has won yet
                        if (teamWon == 1000)
                        {
                            uint id = (uint)i;
                            teamWon = id;
                        }
                        //Another team has as many points as we have
                        else
                        {
                            //That means draw!
                            teamWon = 999;
                            break;
                        }
                    }
                }

                //End game according to results
                main.EndGame((uint)teamWon, drd.teamPoints.ToArray());
            }
            //Victory screen is over. Proceed to map voting.
            else if (main.gameModeStage == 2)
            {
                //Destroy victory screen
                if (main.currentVictoryScreen)
                {
                    NetworkServer.Destroy(main.currentVictoryScreen.gameObject);
                }
                if (Kit_GameSettings.currentNetworkingMode == KitNetworkingMode.Traditional)
                {
                    //Set time and stage
                    main.timer = mapVotingTime;
                    main.gameModeStage = 3;
                    //Open the voting menu
                    main.OpenVotingMenu();
                    //Delete all players
                    main.DeleteAllPlayers();
                }
                else if (Kit_GameSettings.currentNetworkingMode == KitNetworkingMode.Lobby)
                {
                    //Delete all players
                    main.DeleteAllPlayers();
                    main.gameModeStage = 5;
                    //Load MM
                    sceneSyncer.LoadScene("MainMenu");
                }
            }
            //End countdown is over, start new game
            else if (main.gameModeStage == 3)
            {
                main.gameModeStage = 4;

                //Lets load the appropriate map
                //Get combo
                MapGameModeCombo nextCombo = main.currentMapVoting.GetComboWithMostVotes();

                networkGameInformation.gameMode = nextCombo.gameMode;
                networkGameInformation.map = nextCombo.map;

                if (main.gameInformation.allPvpGameModes.Length > nextCombo.gameMode)
                {
                    if (main.gameInformation.allPvpGameModes[nextCombo.gameMode].traditionalMaps.Length > nextCombo.map)
                    {
                        //Load the map
                        sceneSyncer.LoadScene(main.gameInformation.allPvpGameModes[nextCombo.gameMode].traditionalMaps[nextCombo.map].sceneName);
                    }
                    else
                    {
                        DebugLog("Map out of range!");
                        //Load the map
                        sceneSyncer.LoadScene(main.gameInformation.allPvpGameModes[nextCombo.gameMode].traditionalMaps[0].sceneName);
                    }
                }
                else
                {
                    DebugLog("Game mode out of range!");
                    //Load the map
                    sceneSyncer.LoadScene(main.gameInformation.allPvpGameModes[0].traditionalMaps[0].sceneName);
                }
            }
        }

        public override bool CanSpawn(Kit_Player player)
        {
            //Check if game stage allows spawning
            if (main.gameModeStage < 2)
            {
                //Check if it is a valid team
                if (player.team >= 0 && player.team < main.gameInformation.allPvpTeams.Length) return true;
            }
            return false;
        }

        public override bool CanControlPlayer()
        {
            //While we are waiting for enough players, we can move!
            if (!AreEnoughPlayersThere() && !main.hasGameModeStarted) return true;
            //We can only control our player if we are in the main phase
            return main.gameModeStage == 1;
        }

        public override bool AreEnoughPlayersThere()
        {
            //If there are bots ...
            if (networkGameInformation.bots)
            {
                return true;
            }
            else
            {
                if (networkGameInformation.isLobby)
                {
                    if (players >= main.currentPvPGameModeBehaviour.lobbyMinimumPlayersNeeded)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    //If there are 2 or more players, we can play!
                    if (players >= main.currentPvPGameModeBehaviour.traditionalPlayerNeeded[networkGameInformation.playersNeeded])
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public override void GameModeBeginMiddle()
        {
            //Reset all states
            if (main.currentGameModeRuntimeData != null && main.currentGameModeRuntimeData.GetType() == typeof(Kit_PvP_GMB_DominationNetworkData))
            {
                Kit_PvP_GMB_DominationNetworkData drd = main.currentGameModeRuntimeData as Kit_PvP_GMB_DominationNetworkData;
                //Reset score
                for (int i = 0; i < drd.teamPoints.Count; i++)
                {
                    drd.teamPoints[i] = 0;
                }
                drd.lastTick = 0f;
                //Reset flags (material should change automatically next frame)
                for (int i = 0; i < drd.flags.Count; i++)
                {
                    drd.flags[i].captureProgress = 0f;
                    drd.flags[i].currentOwner = 0;
                    drd.flags[i].currentState = 0;
                    drd.flags[i].playersCapturingFlag = 0;
                }
            }

            base.GameModeBeginMiddle();
        }

        public override bool ArePlayersEnemies(Kit_PlayerBehaviour playerOne, Kit_PlayerBehaviour playerTwo)
        {
            if (playerOne.myTeam != playerTwo.myTeam) return true;
            return false;
        }

        public override bool ArePlayersEnemies(uint playerOneID, bool playerOneBot, uint playerTwoID, bool playerTwoBot, bool canKillSelf = false)
        {
            if (playerTwoBot && playerOneBot && playerOneID == playerTwoID && canKillSelf) return true;

            int teamOne = -1;
            int teamTwo = -2;

            if (playerOneBot)
            {
                Kit_Bot bot = main.currentBotManager.GetBotWithID(playerOneID);
                teamOne = bot.team;
            }
            else
            {
                Kit_Player player = networkPlayerManager.GetPlayerById(playerOneID);
                teamOne = player.team;
            }

            if (playerTwoBot)
            {
                Kit_Bot bot = main.currentBotManager.GetBotWithID(playerTwoID);
                teamTwo = bot.team;
            }
            else
            {
                Kit_Player player = networkPlayerManager.GetPlayerById(playerTwoID);
                teamTwo = player.team;
            }

            if (teamOne != teamTwo) return true;

            return false;
        }

        public override bool ArePlayersEnemies(uint playerOneID, bool playerOneBot, Kit_PlayerBehaviour playerTwo, bool canKillSelf)
        {
            if (playerTwo.isBot == playerOneBot && playerOneID == playerTwo.id && canKillSelf) return true;

            int oneTeam = -1;

            if (playerOneBot)
            {
                Kit_Bot bot = main.currentBotManager.GetBotWithID(playerOneID);
                oneTeam = bot.team;
            }
            else
            {
                Kit_Player player = networkPlayerManager.GetPlayerById(playerOneID);
                oneTeam = player.team;
            }

            if (oneTeam != playerTwo.myTeam) return true;
            return false;
        }

        public override bool AreWeEnemies(bool botEnemy, uint enemyId)
        {
            if (!NetworkClient.active) return true;

            //So that we can blind/kill ourselves with grenades
            if (!botEnemy && enemyId == networkPlayerManager.GetLocalPlayer().id) return true;

            int enemyTeam = -1;

            if (botEnemy)
            {
                Kit_Bot bot = main.currentBotManager.GetBotWithID(enemyId);
                if (bot != null)
                    enemyTeam = bot.team;
                else //If he doesn't exist, we can't be enemies
                    return false;
            }
            else
            {
                Kit_Player player = networkPlayerManager.GetPlayerById(enemyId);
                enemyTeam = player.team;
            }

            if (main.assignedTeamID != enemyTeam) return true;
            return false;
        }

        public override bool CanStartVote()
        {
            //While we are waiting for enough players, we can vote!
            if (!AreEnoughPlayersThere() && !main.hasGameModeStarted) return true;
            //We can only vote during the main phase and if enough time is left
            return main.gameModeStage == 1 && main.timer > votingThreshold;
        }

        public override void RegisterNetworkPrefabs()
        {
            if (!NetworkClient.prefabs.ContainsKey(flagPrefab.GetComponent<NetworkIdentity>().assetId))
            {
                NetworkClient.RegisterPrefab(flagPrefab);
            }
        }

#if UNITY_EDITOR
        public override string[] GetSceneCheckerMessages()
        {
            List<string> toReturn = new List<string>();
            //Find spawns
            Kit_PlayerSpawn[] spawns = FindObjectsByType<Kit_PlayerSpawn>(FindObjectsSortMode.None);
            //Get good spawns
            List<Kit_PlayerSpawn> spawnsForThisGameMode = new List<Kit_PlayerSpawn>();
            for (int i = 0; i < spawns.Length; i++)
            {
                if (spawns[i].pvpGameModes.Contains(this))
                {
                    spawnsForThisGameMode.Add(spawns[i]);
                }
            }

            Kit_Domination_Flag[] flags = FindObjectsByType<Kit_Domination_Flag>(FindObjectsSortMode.None);

            int backupSpawns = 0;
            int teamOneSpawns = 0;
            int teamTwoSpawns = 0;
            int[] flagSpawns = new int[flags.Length];

            //Loop through all
            for (int i = 0; i < spawnsForThisGameMode.Count; i++)
            {
                if (spawnsForThisGameMode[i].spawnGroupID == 0)
                {
                    backupSpawns++;
                }
                else if (spawnsForThisGameMode[i].spawnGroupID == 1)
                {
                    teamOneSpawns++;
                }
                else if (spawnsForThisGameMode[i].spawnGroupID == 2)
                {
                    teamTwoSpawns++;
                }

                for (int o = 0; o < flags.Length; o++)
                {
                    if (spawnsForThisGameMode[i].spawnGroupID == 3 + o)
                    {
                        flagSpawns[o]++;
                    }
                }
            }

            //Now add string based on found spawns
            if (backupSpawns == 0)
            {
                toReturn.Add("[Backup Spawns; Spawn Group ID = 0] None found.");
            }
            else if (backupSpawns < 5)
            {
                toReturn.Add("[Backup Spawns; Spawn Group ID = 0] Maybe add a few more?");
            }
            else
            {
                toReturn.Add("[Backup Spawns; Spawn Group ID = 0] All good.");
            }

            if (teamOneSpawns == 0)
            {
                toReturn.Add("[Team One Start Spawns; Spawn Group ID = 1] None found.");
            }
            else if (teamOneSpawns < 5)
            {
                toReturn.Add("[Team One Start Spawns; Spawn Group ID = 1] Maybe add a few more?");
            }
            else
            {
                toReturn.Add("[Team One Start Spawns; Spawn Group ID = 1] All good.");
            }

            if (teamTwoSpawns == 0)
            {
                toReturn.Add("[Team Two Start Spawns; Spawn Group ID = 2] None found.");
            }
            else if (teamTwoSpawns < 5)
            {
                toReturn.Add("[Team Two Start Spawns; Spawn Group ID = 2] Maybe add a few more?");
            }
            else
            {
                toReturn.Add("[Team Two Start Spawns; Spawn Group ID = 2] All good.");
            }

            if (flags.Length <= 0)
            {
                toReturn.Add("[Flags] No flags found.");
            }
            else if (flags.Length <= 2)
            {
                toReturn.Add("[Flags] Maybe add a few more?");
            }
            else
            {
                toReturn.Add("[Flags] All Good.");
            }

            for (int i = 0; i < flagSpawns.Length; i++)
            {
                if (flagSpawns[i] == 0)
                {
                    toReturn.Add("[Flag #" + i + "; Spawn Group ID = " + (3 + i) + "] None found.");
                }
                else if (flagSpawns[i] < 5)
                {
                    toReturn.Add("[Flag #" + i + "; Spawn Group ID = " + (3 + i) + "] Maybe add a few more?");
                }
                else
                {
                    toReturn.Add("[Flag #" + i + "; Spawn Group ID = " + (3 + i) + "] All good.");
                }
            }

            Kit_BotNavPoint[] navPoints = FindObjectsByType<Kit_BotNavPoint>(FindObjectsSortMode.None);
            List<Kit_BotNavPoint> navPointsForThis = new List<Kit_BotNavPoint>();

            for (int i = 0; i < navPoints.Length; i++)
            {
                if (navPoints[i].gameModes.Contains(this))
                {
                    navPointsForThis.Add(navPoints[i]);
                }
            }

            int backupNavPoints = 0;
            int[] flagNavPoints = new int[flags.Length];

            //Loop through all
            for (int i = 0; i < navPointsForThis.Count; i++)
            {
                if (navPointsForThis[i].navPointGroupID == 0)
                {
                    backupNavPoints++;
                }

                for (int o = 0; o < flags.Length; o++)
                {
                    if (navPointsForThis[i].navPointGroupID == 1 + o)
                    {
                        flagNavPoints[o]++;
                    }
                }
            }

            if (backupNavPoints <= 0)
            {
                toReturn.Add("[Backup Nav Points] No nav points for this game mode found!");
            }
            else if (backupNavPoints <= 6)
            {
                toReturn.Add("[Backup Nav Points] Maybe you should add a few more");
            }
            else
            {
                toReturn.Add("[Backup Nav Points] All good.");
            }

            for (int i = 0; i < flagSpawns.Length; i++)
            {
                if (flagNavPoints[i] == 0)
                {
                    toReturn.Add("[Flag #" + i + "; Nav Point Group ID = " + (1 + i) + "] None found.");
                }
                else if (flagNavPoints[i] < 5)
                {
                    toReturn.Add("[Flag #" + i + "; Nav Point Group ID = " + (1 + i) + "] Maybe add a few more?");
                }
                else
                {
                    toReturn.Add("[Flag #" + i + "; Nav Point Group ID = " + (1 + i) + "] All good.");
                }
            }


            return toReturn.ToArray();
        }

        public override MessageType[] GetSceneCheckerMessageTypes()
        {
            List<MessageType> toReturn = new List<MessageType>();
            //Find spawns
            Kit_PlayerSpawn[] spawns = FindObjectsByType<Kit_PlayerSpawn>(FindObjectsSortMode.None);
            //Get good spawns
            List<Kit_PlayerSpawn> spawnsForThisGameMode = new List<Kit_PlayerSpawn>();
            for (int i = 0; i < spawns.Length; i++)
            {
                if (spawns[i].pvpGameModes.Contains(this))
                {
                    spawnsForThisGameMode.Add(spawns[i]);
                }
            }

            Kit_Domination_Flag[] flags = FindObjectsByType<Kit_Domination_Flag>(FindObjectsSortMode.None);

            int backupSpawns = 0;
            int teamOneSpawns = 0;
            int teamTwoSpawns = 0;
            int[] flagSpawns = new int[flags.Length];

            //Loop through all
            for (int i = 0; i < spawnsForThisGameMode.Count; i++)
            {
                if (spawnsForThisGameMode[i].spawnGroupID == 0)
                {
                    backupSpawns++;
                }
                else if (spawnsForThisGameMode[i].spawnGroupID == 1)
                {
                    teamOneSpawns++;
                }
                else if (spawnsForThisGameMode[i].spawnGroupID == 2)
                {
                    teamTwoSpawns++;
                }

                for (int o = 0; o < flags.Length; o++)
                {
                    if (spawnsForThisGameMode[i].spawnGroupID == 3 + o)
                    {
                        flagSpawns[o]++;
                    }
                }
            }

            //Now add string based on found spawns
            if (backupSpawns == 0)
            {
                toReturn.Add(MessageType.Error);
            }
            else if (backupSpawns < 5)
            {
                toReturn.Add(MessageType.Warning);
            }
            else
            {
                toReturn.Add(MessageType.Info);
            }

            if (teamOneSpawns == 0)
            {
                toReturn.Add(MessageType.Error);
            }
            else if (teamOneSpawns < 5)
            {
                toReturn.Add(MessageType.Warning);
            }
            else
            {
                toReturn.Add(MessageType.Info);
            }

            if (teamTwoSpawns == 0)
            {
                toReturn.Add(MessageType.Error);
            }
            else if (teamTwoSpawns < 5)
            {
                toReturn.Add(MessageType.Warning);
            }
            else
            {
                toReturn.Add(MessageType.Info);
            }

            if (flags.Length <= 0)
            {
                toReturn.Add(MessageType.Error);
            }
            else if (flags.Length <= 2)
            {
                toReturn.Add(MessageType.Warning);
            }
            else
            {
                toReturn.Add(MessageType.Info);
            }

            for (int i = 0; i < flagSpawns.Length; i++)
            {
                if (flagSpawns[i] == 0)
                {
                    toReturn.Add(MessageType.Error);
                }
                else if (flagSpawns[i] < 5)
                {
                    toReturn.Add(MessageType.Warning);
                }
                else
                {
                    toReturn.Add(MessageType.Info);
                }
            }


            Kit_BotNavPoint[] navPoints = FindObjectsByType<Kit_BotNavPoint>(FindObjectsSortMode.None);
            List<Kit_BotNavPoint> navPointsForThis = new List<Kit_BotNavPoint>();

            for (int i = 0; i < navPoints.Length; i++)
            {
                if (navPoints[i].gameModes.Contains(this))
                {
                    navPointsForThis.Add(navPoints[i]);
                }
            }

            int backupNavPoints = 0;
            int[] flagNavPoints = new int[flags.Length];

            //Loop through all
            for (int i = 0; i < navPointsForThis.Count; i++)
            {
                if (navPointsForThis[i].navPointGroupID == 0)
                {
                    backupNavPoints++;
                }

                for (int o = 0; o < flags.Length; o++)
                {
                    if (navPointsForThis[i].navPointGroupID == 1 + o)
                    {
                        flagNavPoints[o]++;
                    }
                }
            }

            if (backupNavPoints <= 0)
            {
                toReturn.Add(MessageType.Error);
            }
            else if (backupNavPoints <= 6)
            {
                toReturn.Add(MessageType.Warning);
            }
            else
            {
                toReturn.Add(MessageType.Info);
            }

            for (int i = 0; i < flagSpawns.Length; i++)
            {
                if (flagNavPoints[i] == 0)
                {
                    toReturn.Add(MessageType.Error);
                }
                else if (flagNavPoints[i] < 5)
                {
                    toReturn.Add(MessageType.Warning);
                }
                else
                {
                    toReturn.Add(MessageType.Info);
                }
            }
            return toReturn.ToArray();
        }
#endif
    }
}
