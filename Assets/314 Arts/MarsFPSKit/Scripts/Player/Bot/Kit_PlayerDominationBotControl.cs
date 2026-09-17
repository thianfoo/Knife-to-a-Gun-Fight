
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace MarsFPSKit
{
    [CreateAssetMenu(menuName = "MarsFPSKit/Bots/Behaviour/Domination Behaviour")]
    public class Kit_PlayerDominationBotControl : Kit_PlayerBotControlBase
    {
        [Header("Spotting")]
        public LayerMask spottingLayer;
        public LayerMask spottingCheckLayers;
        public float spottingMaxDistance = 50f;
        public Vector2 spottingBoxExtents = new Vector2(30, 30);
        private Vector3 spottingBoxSize;
        public float spottingFov = 90f;
        public float spottingRayDistance = 200f;
        /// <summary>
        /// How fast does the bot look?
        /// </summary>
        public float lookingInputSmooth = 5f;
        /// <summary>
        /// How fast does the bot change his direction?
        /// </summary>
        public float walkingInputSmooth = 5f;

        [Header("Shooting")]
        public float shootingAngleDifference = 5f;

        public int useLength = 2;

        public override void InitializeControls(Kit_PlayerBehaviour pb)
        {
            Kit_PlayerDominationBotControlData kbcrd = new GameObject().AddComponent<Kit_PlayerDominationBotControlData>();
            kbcrd.gameObject.name = pb.gameObject.name + " (Bot Controls)";
            //Assign runtime data
            pb.botControlsRuntimeData = kbcrd;
            //Add Navmesh Agent
            kbcrd.nma = pb.gameObject.AddComponent<NavMeshAgent>();
            //Setup NMA
            kbcrd.nma.updatePosition = false;
            kbcrd.nma.updateRotation = false;
            kbcrd.nma.updateUpAxis = false;
            kbcrd.nma.nextPosition = pb.transform.position;
            SetDestination(kbcrd, pb.transform.position);
            kbcrd.nma.speed = 0f;

            kbcrd.aimSkill = Random.Range(-0.4f, 0.4f);
            kbcrd.enemyPlayersAwareOff = new List<GameObject>();
            spottingBoxSize = new Vector3(spottingBoxExtents.x, spottingBoxExtents.y, spottingMaxDistance / 2f);

            kbcrd.lookTarget = pb.transform.position + pb.transform.forward * 5f;


            pb.input.weaponSlotUses = new bool[useLength];
            //First action
            ReachedDestination(pb, kbcrd);
        }

        public override void WriteToPlayerInput(Kit_PlayerBehaviour pb)
        {
            Kit_PlayerDominationBotControlData kbcrd = pb.botControlsRuntimeData as Kit_PlayerDominationBotControlData;

            if (pb.enableInput)
            {
                kbcrd.nma.speed = 3;

                if (Time.time - 0.1f > kbcrd.lastRoutine)
                {
                    kbcrd.lastRoutine = Time.time;
                    CheckForEnemies(pb, kbcrd);
                    UpdateAimPosition(pb, kbcrd);
                    FiringDecision(pb, kbcrd);

                    if (!kbcrd.reachedDestination)
                    {
                        if (GetRemainingDistance(kbcrd) <= 0.5f)
                        {
                            kbcrd.reachedDestination = true;
                            ReachedDestination(pb, kbcrd);
                        }
                    }

                    if (!kbcrd.playerToEngage)
                    {
                        if (GetRemainingDistance(kbcrd) > 10f && !pb.input.lmb)
                        {
                            pb.input.sprint = true;
                        }
                        else
                        {
                            pb.input.sprint = false;
                        }
                    }
                    else
                    {
                        pb.input.sprint = false;
                    }

                    if (Time.time > kbcrd.lastForceNewWaypoint || !kbcrd.playerToEngage && kbcrd.nma.isOnNavMesh && (GetRemainingDistance(kbcrd) < 3f || !kbcrd.nma.hasPath))
                    {
                        kbcrd.lastForceNewWaypoint = Time.time + 3;
                        if (!kbcrd.playerToEngage)
                        {
                            if (!kbcrd.capturingFlag)
                            {
                                var capture = GetNearestEnemyOwnedFlag(pb, kbcrd);
                                if (capture)
                                {
                                    if (kbcrd.nextWaypointAction == 0)
                                    {
                                        Kit_BotNavPoint navPoint = capture.GetNavPoint();
                                        //Set new waypoint
                                        SetDestination(kbcrd, navPoint.transform.position);
                                        //Set ID
                                        kbcrd.capturingFlag = capture;
                                        kbcrd.capturingFlagNavPoint = navPoint;
                                        kbcrd.nextWaypointAction = 1;
                                        //Check for enemies
                                        CheckForEnemies(pb, kbcrd);
                                    }
                                }
                                else
                                {
                                    //Set new waypoint
                                    SetDestination(kbcrd, main.botNavPoints[Random.Range(0, main.botNavPoints.Length)].position);
                                    kbcrd.nextWaypointAction = 0;
                                }
                            }
                            else
                            {
                                if (kbcrd.capturingFlag.currentOwner == pb.myTeam + 1)
                                {
                                    kbcrd.capturingFlag.ReleaseNavPoint(kbcrd.capturingFlagNavPoint);
                                    kbcrd.nextWaypointAction = 0;
                                    kbcrd.capturingFlag = null;
                                    kbcrd.capturingFlagNavPoint = null;
                                    //Set new waypoint
                                    SetDestination(kbcrd, main.botNavPoints[Random.Range(0, main.botNavPoints.Length)].position);
                                    kbcrd.nextWaypointAction = 0;
                                }
                            }
                        }
                        else
                        {
                            if (kbcrd.capturingFlag)
                            {
                                kbcrd.capturingFlag.ReleaseNavPoint(kbcrd.capturingFlagNavPoint);
                                kbcrd.capturingFlag = null;
                                kbcrd.capturingFlagNavPoint = null;
                                kbcrd.nextWaypointAction = 0;
                            }

                            if (Vector3.Distance(pb.transform.position, kbcrd.playerToEngage.transform.position) > 10f)
                            {
                                SetDestination(kbcrd, kbcrd.playerToEngage.transform.position + (Random.insideUnitSphere * Random.Range(1, 5f)));
                            }
                            else
                            {
                                int nearestWaypoint = Random.Range(0, main.botNavPoints.Length);
                                float maxDistance = float.MaxValue;

                                for (int i = 0; i < main.botNavPoints.Length; i++)
                                {
                                    float dist = Vector3.Distance(kbcrd.playerToEngage.transform.position, main.botNavPoints[i].transform.position);
                                    if (dist < maxDistance)
                                    {
                                        if (Vector3.Distance(pb.transform.position, main.botNavPoints[i].transform.position) > 10f)
                                        {
                                            maxDistance = dist;
                                            int id = i;
                                            nearestWaypoint = id;
                                        }
                                    }
                                }
                                //Set new waypoint
                                SetDestination(kbcrd, main.botNavPoints[nearestWaypoint].position + (Random.insideUnitSphere * Random.Range(1, 3f)));
                            }
                        }
                    }
                }

                #region Looking
                //Smooth look target
                kbcrd.smoothedLookTarget = Vector3.Lerp(kbcrd.smoothedLookTarget, kbcrd.lookTarget, Time.deltaTime * lookingInputSmooth);
                //Look at target
                Vector3 target = kbcrd.smoothedLookTarget;
                //Only look on y axis
                target.y = 0;
                //Get current
                Vector3 current = pb.transform.position;
                //Only look on y axis
                current.y = 0;
                if (target == current)
                {

                }
                else
                {
                    pb.transform.rotation = Quaternion.LookRotation(target - current);
                }
                //Reset
                target = kbcrd.smoothedLookTarget;
                current = pb.mouseLookObject.position;
                pb.mouseLookObject.rotation = Quaternion.LookRotation(target - current);
                #endregion
                //Send NavMeshAgent input to character controller
                kbcrd.walkInput = Vector3.Lerp(kbcrd.walkInput, pb.transform.InverseTransformDirection(kbcrd.nma.desiredVelocity.normalized), Time.deltaTime * walkingInputSmooth);
                //Copy input and smooth it
                pb.input.hor = kbcrd.walkInput.x;
                pb.input.ver = kbcrd.walkInput.z;
                //copy velocity back
                kbcrd.nma.velocity = pb.cc.velocity;
                if (kbcrd.nma.desiredVelocity.sqrMagnitude > 1)
                    kbcrd.nma.nextPosition = pb.transform.position;
            }
            else
            {
                kbcrd.nma.speed = 0;

                //Reset all input
                pb.input.hor = 0f;
                pb.input.ver = 0f;

                pb.input.crouch = false;
                pb.input.sprint = false;
                pb.input.jump = false;
                pb.input.interact = false;

                pb.input.rmb = false;
                pb.input.reload = false;

                pb.input.leanLeft = false;
                pb.input.leanRight = false;
                pb.input.thirdPerson = false;
                pb.input.flashlight = false;
                pb.input.laser = false;

                pb.input.lmb = false;
            }

            //Set Camera (this is validated on the server)
            pb.input.clientCamPos = pb.playerCameraTransform.position;
            pb.input.clientCamForward = pb.playerCameraTransform.forward;
        }

        private void SetDestination(Kit_PlayerDominationBotControlData kbcrd, Vector3 position)
        {
            if (kbcrd.nma.isOnNavMesh)
            {
                kbcrd.nma.SetDestination(position);
            }
        }

        private float GetRemainingDistance(Kit_PlayerDominationBotControlData kbcrd)
        {
            if (kbcrd.nma.isOnNavMesh)
            {
                return kbcrd.nma.remainingDistance;
            }
            else
            {
                return float.MaxValue;
            }
        }

        public void FlagCaptured(Kit_PlayerBehaviour pb, Kit_Domination_FlagRuntime flag, int whoWhoCaptured)
        {
            Kit_PlayerDominationBotControlData kbcrd = pb.botControlsRuntimeData as Kit_PlayerDominationBotControlData;

            if (flag == kbcrd.capturingFlag)
            {
                kbcrd.nextWaypointAction = 0;
                var capture = GetNearestEnemyOwnedFlag(pb, kbcrd);
                if (capture)
                {
                    Kit_BotNavPoint navPoint = capture.GetNavPoint();
                    //Set new waypoint
                    SetDestination(kbcrd, navPoint.transform.position);
                    //Set ID
                    kbcrd.capturingFlag = capture;
                    kbcrd.capturingFlagNavPoint = navPoint;
                    kbcrd.nextWaypointAction = 1;
                    //Check for enemies
                    CheckForEnemies(pb, kbcrd);
                }
                else
                {
                    //Set new waypoint
                    SetDestination(kbcrd, main.botNavPoints[Random.Range(0, main.botNavPoints.Length)].position);
                    kbcrd.nextWaypointAction = 0;
                }
                kbcrd.reachedDestination = false;
            }
            else
            {
                if (kbcrd.nextWaypointAction != 1 && !kbcrd.playerToEngage && whoWhoCaptured != pb.myTeam)
                {
                    var capture = GetNearestEnemyOwnedFlag(pb, kbcrd);
                    if (capture)
                    {
                        Kit_BotNavPoint navPoint = capture.GetNavPoint();
                        //Set new waypoint
                        SetDestination(kbcrd, navPoint.transform.position);
                        //Set ID
                        kbcrd.capturingFlag = capture;
                        kbcrd.capturingFlagNavPoint = navPoint;
                        kbcrd.nextWaypointAction = 1;
                        //Check for enemies
                        CheckForEnemies(pb, kbcrd);
                    }
                    else
                    {
                        //Set new waypoint
                        SetDestination(kbcrd, main.botNavPoints[Random.Range(0, main.botNavPoints.Length)].position);
                        kbcrd.nextWaypointAction = 0;
                    }
                }
            }
        }

        void ReachedDestination(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd)
        {
            if (!kbcrd.playerToEngage)
            {
                if (kbcrd.nextWaypointAction == 0)
                {
                    var capture = GetNearestEnemyOwnedFlag(pb, kbcrd);
                    if (capture)
                    {
                        if (kbcrd.nextWaypointAction == 0)
                        {
                            Kit_BotNavPoint navPoint = capture.GetNavPoint();
                            //Set new waypoint
                            SetDestination(kbcrd, navPoint.transform.position);
                            //Set ID
                            kbcrd.capturingFlag = capture;
                            kbcrd.capturingFlagNavPoint = navPoint;
                            kbcrd.nextWaypointAction = 1;
                            //Check for enemies
                            CheckForEnemies(pb, kbcrd);
                            kbcrd.reachedDestination = true;
                        }
                    }
                    else
                    {
                        //Set new waypoint
                        SetDestination(kbcrd, main.botNavPoints[Random.Range(0, main.botNavPoints.Length)].position);
                        kbcrd.nextWaypointAction = 0;
                        kbcrd.reachedDestination = false;
                    }
                }
                else if (kbcrd.nextWaypointAction == 1)
                {
                    //Wait and do nothing
                    //kbcrd.nma.destination = pb.transform.position;
                }
            }
            else
            {
                if (Vector3.Distance(pb.transform.position, kbcrd.playerToEngage.transform.position) > 10f)
                {
                    SetDestination(kbcrd, kbcrd.playerToEngage.transform.position + (Random.insideUnitSphere * Random.Range(1, 5f)));
                }
                else
                {
                    int nearestWaypoint = Random.Range(0, main.botNavPoints.Length);
                    float maxDistance = float.MaxValue;

                    for (int i = 0; i < main.botNavPoints.Length; i++)
                    {
                        float dist = Vector3.Distance(kbcrd.playerToEngage.transform.position, main.botNavPoints[i].transform.position);
                        if (dist < maxDistance)
                        {
                            if (Vector3.Distance(pb.transform.position, main.botNavPoints[i].transform.position) > 10f)
                            {
                                maxDistance = dist;
                                int id = i;
                                nearestWaypoint = id;
                            }
                        }
                    }
                    //Set new waypoint
                    SetDestination(kbcrd, main.botNavPoints[nearestWaypoint].position + (Random.insideUnitSphere * Random.Range(1, 3f)));
                }
                kbcrd.reachedDestination = false;
                /*
                kbcrd.nma.SetDestination(kbcrd.playerToEngage.transform.position + (Random.insideUnitSphere * Random.Range(0, 3f)));
                kbcrd.reachedDestination = false;
                */
            }
        }

        Kit_Domination_FlagRuntime GetNearestEnemyOwnedFlag(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd)
        {
            Kit_PvP_GMB_DominationNetworkData drd = main.currentGameModeRuntimeData as Kit_PvP_GMB_DominationNetworkData;
            Kit_Domination_FlagRuntime nearestFlag = null;
            float nearestFlagDistance = float.MaxValue;
            for (int i = 0; i < drd.flags.Count; i++)
            {
                //Check if its enemy flag
                if (!(drd.flags[i].currentOwner == pb.myTeam + 1))
                {
                    //Check distance
                    if (Vector3.Distance(pb.transform.position, drd.flags[i].transform.position) < nearestFlagDistance)
                    {
                        nearestFlagDistance = Vector3.Distance(pb.transform.position, drd.flags[i].transform.position);
                        nearestFlag = drd.flags[i];
                    }
                }
            }

            return nearestFlag;
        }

        void CheckForEnemies(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd)
        {
            Collider[] possiblePlayers = Physics.OverlapBox(pb.playerCameraTransform.position + pb.playerCameraTransform.forward * (spottingMaxDistance / 2), spottingBoxSize, pb.playerCameraTransform.rotation, spottingLayer.value);

            //Clean
            kbcrd.enemyPlayersAwareOff.RemoveAll(item => item == null);

            //Loop
            for (int i = 0; i < possiblePlayers.Length; i++)
            {
                //Check if it is a player
                Kit_PlayerBehaviour pnb = possiblePlayers[i].transform.root.GetComponent<Kit_PlayerBehaviour>();
                if (pnb && pnb != pb)
                {
                    if (CanSeePlayer(pb, kbcrd, pnb.gameObject))
                    {
                        if (isEnemyPlayer(pb, kbcrd, pnb.gameObject))
                        {
                            if (!kbcrd.enemyPlayersAwareOff.Contains(pnb.gameObject))
                            {
                                //Add to our known list
                                kbcrd.enemyPlayersAwareOff.Add(pnb.gameObject);
                                //Call spotted
                                if (pb.voiceManager)
                                {
                                    pb.voiceManager.SpottedEnemy(pb, pnb);
                                }
                            }
                        }
                    }
                }
            }
        }

        bool CanSeePlayer(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd, GameObject enemyPlayer)
        {
            if (enemyPlayer)
            {
                Kit_PlayerBehaviour enemyPb = enemyPlayer.GetComponent<Kit_PlayerBehaviour>();
                RaycastHit hit;
                Vector3 rayDirection = Vector3.zero;
                if (enemyPb)
                {
                    rayDirection = enemyPb.playerCameraTransform.position - new Vector3(0, 0.2f, 0f) - pb.playerCameraTransform.position;
                }

                if ((Vector3.Angle(rayDirection, pb.playerCameraTransform.forward)) < spottingFov)
                {
                    if (Physics.Raycast(pb.playerCameraTransform.position, rayDirection, out hit, spottingRayDistance, spottingCheckLayers.value))
                    {
                        if (hit.collider.transform.root == enemyPlayer.transform.root)
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
                        return true;
                    }
                }
            }

            return false;
        }

        bool isEnemyPlayer(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd, GameObject enemyPlayer)
        {
            if (pb)
            {
                if (!pb.syncSetup) return false;
                if (main.currentPvPGameModeBehaviour)
                {
                    if (!main.currentPvPGameModeBehaviour.isTeamGameMode) return true;
                    else
                    {
                        Kit_PlayerBehaviour enemyPb = enemyPlayer.GetComponent<Kit_PlayerBehaviour>();

                        if (enemyPb)
                        {
                            if (pb.myTeam != enemyPb.myTeam) return true;
                        }
                        else return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        void UpdateAimPosition(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd)
        {
            if (pb.input.sprint)
            {
                kbcrd.lookTarget = pb.playerCameraTransform.position + kbcrd.nma.desiredVelocity.normalized * 5f;
            }
            else
            {
                if (kbcrd.playerToEngage)
                {
                    if (Time.time > kbcrd.lastRandomAimSet + 0.5f)
                    {
                        kbcrd.lastRandomAimSet = Time.time;
                        kbcrd.randomAimAdd = Random.insideUnitSphere * kbcrd.aimSkill;
                    }

                    Kit_PlayerBehaviour enemyPlayer = kbcrd.playerToEngage.GetComponent<Kit_PlayerBehaviour>();
                    if (enemyPlayer)
                    {
                        kbcrd.lookTarget = enemyPlayer.playerCameraTransform.position - new Vector3(0, 0.5f, 0f) + kbcrd.randomAimAdd;
                    }
                }
                else if (kbcrd.nma.desiredVelocity.sqrMagnitude > 1)
                {
                    kbcrd.lookTarget = pb.playerCameraTransform.position + kbcrd.nma.desiredVelocity.normalized * 5f;
                }
                else
                {
                    kbcrd.lookTarget = pb.playerCameraTransform.position + pb.transform.forward * 5f;
                }
            }
        }

        void FiringDecision(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd)
        {
            int lastWeaponState = pb.weaponManager.WeaponState(pb);
            int lastWeaponType = pb.weaponManager.WeaponType(pb);

            if (lastWeaponState == 0)
            {
                if (Time.time - 5f > kbcrd.lastEngagePlayerSet)
                {
                    UpdatePlayerToEngage(pb, kbcrd);
                }
                else if (!kbcrd.playerToEngage)
                {
                    UpdatePlayerToEngage(pb, kbcrd);
                }

                if (kbcrd.playerToEngage)
                {
                    /*
                    if (Time.time > kbcrd.lastEngagePosSet)
                    {
                        kbcrd.nma.SetDestination(kbcrd.playerToEngage.transform.position + (Random.insideUnitSphere * Random.Range(0, 3f)));
                        kbcrd.lastEngagePosSet = Time.time + Random.Range(2.5f, 15f);
                    }
                    */

                    if (isAimingNearEngage(pb, kbcrd) && CanSeePlayer(pb, kbcrd, kbcrd.playerToEngage))
                    {
                        if (lastWeaponType == 0)
                        {
                            Kit_PlayerBehaviour enemyPlayer = kbcrd.playerToEngage.GetComponent<Kit_PlayerBehaviour>();
                            //Check distance
                            float distance = 0f;
                            if (enemyPlayer)
                            {
                                distance = Vector3.Distance(pb.playerCameraTransform.position, enemyPlayer.playerCameraTransform.position);
                            }

                            //Short distance - spray
                            if (distance < 7.5f)
                            {
                                FullAutoFire(pb, kbcrd);
                                pb.input.rmb = false;
                            }
                            //Medium distance - burst
                            else if (distance < 30f)
                            {
                                BurstFire(pb, kbcrd);
                                pb.input.rmb = true;
                            }
                            //Long distance - single shots
                            else
                            {
                                SingleFire(pb, kbcrd);
                                pb.input.rmb = true;
                            }
                            pb.input.reload = false;
                        }
                        else if (lastWeaponType == 1)
                        {
                            SingleFire(pb, kbcrd);
                            pb.input.reload = false;
                            pb.input.rmb = true;
                        }
                        else if (lastWeaponType == 2)
                        {
                            Kit_PlayerBehaviour enemyPlayer = kbcrd.playerToEngage.GetComponent<Kit_PlayerBehaviour>();
                            //Check distance
                            float distance = 0f;
                            if (enemyPlayer)
                            {
                                distance = Vector3.Distance(pb.playerCameraTransform.position, enemyPlayer.playerCameraTransform.position);
                            }

                            if (distance < 15f)
                            {
                                SingleFire(pb, kbcrd);
                                pb.input.rmb = true;
                            }
                            else
                            {
                                pb.input.rmb = false;
                            }

                            pb.input.reload = false;
                        }
                    }
                    else
                    {
                        pb.input.lmb = false;
                        pb.input.reload = false;
                        pb.input.rmb = false;
                    }
                }
                else
                {
                    pb.input.lmb = false;
                    pb.input.reload = false;
                    pb.input.rmb = false;
                }
            }
            else if (lastWeaponState == 1)
            {
                pb.input.lmb = false;
                if (Time.time > kbcrd.lastReloadTry)
                {
                    pb.input.reload = true;
                    if (Time.time - 0.1f > kbcrd.lastReloadTry)
                    {
                        kbcrd.lastReloadTry = Time.time + 0.2f;
                    }
                }
                else
                {
                    pb.input.reload = false;
                }
                pb.input.rmb = false;
            }
            else if (lastWeaponState == 2)
            {
                pb.input.weaponSlotUses[1] = true;
                pb.input.lmb = false;
                pb.input.reload = false;
                pb.input.rmb = false;
            }
        }

        void UpdatePlayerToEngage(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd)
        {
            kbcrd.lastEngagePlayerSet = Time.time;
            kbcrd.enemyPlayersAwareOff = kbcrd.enemyPlayersAwareOff.OrderBy(x => Vector3.Distance(pb.transform.position, x.transform.position)).ToList();

            for (int i = 0; i < kbcrd.enemyPlayersAwareOff.Count; i++)
            {
                if (i < kbcrd.enemyPlayersAwareOff.Count)
                {
                    if (CanSeePlayer(pb, kbcrd, kbcrd.enemyPlayersAwareOff[i]))
                    {
                        kbcrd.playerToEngage = kbcrd.enemyPlayersAwareOff[i];
                        break;
                    }
                }
            }
        }

        void FullAutoFire(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd)
        {
            pb.input.lmb = true;
        }

        void BurstFire(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd)
        {
            if (Time.time > kbcrd.lastShot)
            {
                pb.input.lmb = true;
                kbcrd.burstTime += 0.1f;

                if (kbcrd.burstTime >= 0.5f)
                {
                    kbcrd.lastShot = Time.time + 0.8f;
                }
            }
            else
            {
                pb.input.lmb = false;
            }
        }

        void SingleFire(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd)
        {
            if (Time.time > kbcrd.lastShot)
            {
                pb.input.lmb = false;
                kbcrd.lastShot = Time.time + 0.6f;
            }
            else
            {
                pb.input.lmb = true;
            }
        }

        bool isAimingNearEngage(Kit_PlayerBehaviour pb, Kit_PlayerDominationBotControlData kbcrd)
        {
            if (kbcrd.playerToEngage)
            {
                Kit_PlayerBehaviour enemyPlayer = kbcrd.playerToEngage.GetComponent<Kit_PlayerBehaviour>();
                //Check distance
                if (enemyPlayer)
                {
                    kbcrd.directAimVector = ((enemyPlayer.playerCameraTransform.position - new Vector3(0, 0.2f, 0f)) - pb.playerCameraTransform.position);
                }

                if (Vector3.Angle(kbcrd.directAimVector, pb.playerCameraTransform.forward) < shootingAngleDifference)
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
                return false;
            }
        }

        public override void OnDestroyRelay(Kit_PlayerBehaviour pb)
        {
            Kit_PlayerDominationBotControlData kbcrd = pb.botControlsRuntimeData as Kit_PlayerDominationBotControlData;

            if (kbcrd.capturingFlag)
            {
                kbcrd.capturingFlag.ReleaseNavPoint(kbcrd.capturingFlagNavPoint);
            }
        }
    }
}
