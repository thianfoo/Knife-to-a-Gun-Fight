
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarsFPSKit
{
    namespace Spectating
    {
        /// <summary>
        /// Runtime data for the default spectator manager
        /// </summary>
        public class SpectatorManagerRuntimeData
        {
            /// <summary>
            /// Whomst we are currently spectating
            /// </summary>
            public Kit_PlayerBehaviour currentSpectator;
            /// <summary>
            /// Are we currently in spectating mode?
            /// </summary>
            public bool isSpectating;
            /// <summary>
            /// UI reference
            /// </summary>
            public Kit_SpectatorUI ui;
        }

        [CreateAssetMenu(menuName = "MarsFPSKit/Spectator/New Default Manager")]
        public class Kit_SpectatorManagerDefault : Kit_SpectatorManagerBase
        {
            [Tooltip("Is spectating globally enabled?")]
            /// <summary>
            /// Is spectating globally enabled?
            /// </summary>
            public bool enableSpectating = true;

            /// <summary>
            /// Prefab for spectating ui
            /// </summary>
            public GameObject spectatingUi;

            public override bool IsSpectatingEnabled()
            {
                return enableSpectating;
            }

            public override void Setup()
            {
                //Create runtime data
                SpectatorManagerRuntimeData smrd = new SpectatorManagerRuntimeData();

                //Create UI
                if (spectatingUi)
                {
                    GameObject go = Instantiate(spectatingUi, main.ui_root.transform, false);
                    Kit_SpectatorUI ui = go.GetComponent<Kit_SpectatorUI>();

                    //Setup buttons
                    ui.previousPlayer.onClick.AddListener(delegate { PreviousPlayer(); });
                    ui.nextPlayer.onClick.AddListener(delegate { NextPlayer(); });

                    //Assign
                    smrd.ui = ui;
                    //Disable
                    ui.gameObject.SetActive(false);
                }

                //And assign
                main.spectatorManagerRuntimeData = smrd;
            }

            public override void BeginSpectating(bool leaveTeam)
            {
                SpectatorManagerRuntimeData smrd = main.spectatorManagerRuntimeData as SpectatorManagerRuntimeData;

                if (!smrd.isSpectating)
                {
                    if (!main.currentPvPGameModeBehaviour || (main.currentPvPGameModeBehaviour && main.currentPvPGameModeBehaviour.GetSpectateable() != Spectateable.None))
                    {
                        DebugLog("[Spectator Manager] Starting Spectating Mode");

                        //Destroy our player if necessary
                        if (main.myPlayer)
                        {
                            main.myPlayer.CmdSuicide();
                        }

                        if (leaveTeam)
                        {
                            main.NoTeam();
                        }

                        //Close all menus
                        main.SwitchMenu(main.ingameFadeId);
                        //Proceed pause menu
                        main.pauseMenuState = PauseMenuState.main;

                        //Spectate first player
                        Kit_PlayerBehaviour[] spectateables = GetSpectateablePlayers();

                        if (spectateables.Length > 0)
                        {
                            SetSpectatingPlayer(spectateables[0]);
                        }
                        else
                        {
                            if (smrd.ui)
                            {
                                smrd.ui.currentPlayer.text = "Currently Spectating \n No one.";
                            }
                        }

                        //Enable UI
                        if (smrd.ui)
                        {
                            smrd.ui.gameObject.SetActive(true);
                        }

                        smrd.isSpectating = true;
                        
                    }
                }
                else
                {
                    //Close all menus
                    main.SwitchMenu(main.ingameFadeId);
                    //Proceed pause menu
                    main.pauseMenuState = PauseMenuState.main;
                }
            }

            public override void EndSpectating()
            {
                SpectatorManagerRuntimeData smrd = main.spectatorManagerRuntimeData as SpectatorManagerRuntimeData;

                if (smrd.isSpectating)
                {
                    DebugLog("[Spectator Manager] Ending Spectating Mode");

                    if (smrd.currentSpectator)
                    {
                        smrd.currentSpectator.OnSpectatingEnd();
                    }

                    smrd.currentSpectator = null;
                    smrd.isSpectating = false;

                    //Disable UI
                    if (smrd.ui)
                    {
                        smrd.ui.gameObject.SetActive(false);
                    }
                }
            }

            public override void PlayerWasSpawned(Kit_PlayerBehaviour pb)
            {
                SpectatorManagerRuntimeData smrd = main.spectatorManagerRuntimeData as SpectatorManagerRuntimeData;

                if (smrd != null)
                {
                    if (smrd.isSpectating)
                    {
                        if (!smrd.currentSpectator)
                        {
                            if (CanSpectatePlayer(pb))
                            {
                                SetSpectatingPlayer(pb);
                            }
                        }
                    }
                }
            }

            public override void PlayerWasKilled(Kit_PlayerBehaviour pb)
            {
                SpectatorManagerRuntimeData smrd = main.spectatorManagerRuntimeData as SpectatorManagerRuntimeData;

                if (smrd != null)
                {
                    if (smrd.isSpectating)
                    {
                        //End spectating on that guy
                        if (smrd.currentSpectator == pb)
                        {
                            Kit_PlayerBehaviour[] spectateables = GetSpectateablePlayers();

                            if (spectateables.Length > 1)
                            {
                                //Get index
                                int cur = System.Array.IndexOf(spectateables, pb);
                                //Increase
                                cur++;
                                //Clamp
                                if (cur >= spectateables.Length) cur = 0;

                                //Spectate new guy
                                SetSpectatingPlayer(spectateables[cur]);
                            }
                            else
                            {
                                pb.OnSpectatingEnd();

                                if (smrd.ui)
                                {
                                    smrd.ui.currentPlayer.text = "Currently Spectating \n No one.";
                                }
                            }
                        }
                    }
                }
            }

            public void SetSpectatingPlayer(Kit_PlayerBehaviour toSpectate)
            {
                SpectatorManagerRuntimeData smrd = main.spectatorManagerRuntimeData as SpectatorManagerRuntimeData;

                //Check if we are currently spectating someone
                if (smrd.currentSpectator)
                {
                    smrd.currentSpectator.OnSpectatingEnd();
                }

                //Assign new player
                smrd.currentSpectator = toSpectate;

                //Begin spectating that guy
                if (smrd.currentSpectator)
                {
                    DebugLog("[Specator Manager] Now Spectating " + smrd.currentSpectator.name, smrd.currentSpectator);

                    smrd.currentSpectator.OnSpectatingStart();

                    //Set UI
                    if (smrd.ui)
                    {
                        smrd.ui.currentPlayer.text = "Currently Spectating \n" + smrd.currentSpectator.name;
                    }
                }
            }

            public void PreviousPlayer()
            {
                SpectatorManagerRuntimeData smrd = main.spectatorManagerRuntimeData as SpectatorManagerRuntimeData;

                if (smrd.isSpectating)
                {
                    Kit_PlayerBehaviour[] spectateables = GetSpectateablePlayers();

                    if (spectateables.Length > 0)
                    {
                        if (smrd.currentSpectator)
                        {
                            //Get index
                            int cur = System.Array.IndexOf(spectateables, smrd.currentSpectator);
                            //Increase
                            cur--;
                            //Clamp
                            if (cur < 0) cur = spectateables.Length - 1;
                            if (cur < 0) cur = spectateables.Length - 1;

                            //Spectate new guy
                            SetSpectatingPlayer(spectateables[cur]);
                        }
                        else
                        {
                            //Spectate new guy
                            SetSpectatingPlayer(spectateables[0]);
                        }
                    }
                }
            }

            public void NextPlayer()
            {
                SpectatorManagerRuntimeData smrd = main.spectatorManagerRuntimeData as SpectatorManagerRuntimeData;

                if (smrd.isSpectating)
                {
                    Kit_PlayerBehaviour[] spectateables = GetSpectateablePlayers();

                    if (spectateables.Length > 0)
                    {
                        if (smrd.currentSpectator)
                        {
                            //Get index
                            int cur = System.Array.IndexOf(spectateables, smrd.currentSpectator);
                            //Increase
                            cur++;
                            //Clamp
                            if (cur >= spectateables.Length) cur = 0;

                            //Spectate new guy
                            SetSpectatingPlayer(spectateables[cur]);
                        }
                        else
                        {
                            //Spectate new guy
                            SetSpectatingPlayer(spectateables[0]);
                        }
                    }
                }
            }

            public override bool IsCurrentlySpectating()
            {
                SpectatorManagerRuntimeData smrd = main.spectatorManagerRuntimeData as SpectatorManagerRuntimeData;

                if (smrd != null)
                {
                    return smrd.isSpectating;
                }
                else
                {
                    return false;
                }
            }

            public Kit_PlayerBehaviour[] GetSpectateablePlayers()
            {
                if (main.currentPvPGameModeBehaviour && main.currentPvPGameModeBehaviour.GetSpectateable() == Spectateable.Friendlies)
                {
                    return main.allActivePlayers.Where(x => !main.currentPvPGameModeBehaviour.AreWeEnemies(x.isBot, x.id)).ToArray();
                }
                else if (!main.currentPvPGameModeBehaviour || main.currentPvPGameModeBehaviour.GetSpectateable() == Spectateable.All)
                {
                    return main.allActivePlayers.ToArray();
                }

                return null;
            }

            public bool CanSpectatePlayer(Kit_PlayerBehaviour player)
            {
                if (main.currentPvPGameModeBehaviour && main.currentPvPGameModeBehaviour.GetSpectateable() == Spectateable.Friendlies)
                {
                    return !main.currentPvPGameModeBehaviour.AreWeEnemies(player.isBot, player.id);
                }
                else if (!main.currentPvPGameModeBehaviour || main.currentPvPGameModeBehaviour.GetSpectateable() == Spectateable.All)
                {
                    return true;
                }

                return false;
            }
        }
    }
}