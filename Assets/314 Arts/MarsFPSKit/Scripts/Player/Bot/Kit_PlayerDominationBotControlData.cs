using System.Collections.Generic;
using MarsFPSKit;
using UnityEngine;
using UnityEngine.AI;

namespace MarsFPSKit
{
    public class Kit_PlayerDominationBotControlData : Kit_PlayerBotControlDataBase
    {
        /// <summary>
        /// Navmesh agent used for navigation
        /// </summary>
        public NavMeshAgent nma;
        /// <summary>
        /// Where the bot should look in world space
        /// </summary>
        public Vector3 lookTarget = new Vector3(0, 100, 0);
        /// <summary>   
        /// Smoothed out look target
        /// </summary>
        public Vector3 smoothedLookTarget;

        //Input
        public Vector3 walkInput;

        public bool isEngaging = false;

        public List<GameObject> enemyPlayersAwareOff = new List<GameObject>();

        public GameObject playerToEngage;

        /// <summary>
        /// Max offset on aim
        /// </summary>
        public float aimSkill = 1f;
        /// <summary>
        /// When was the bot updated for the last time?
        /// </summary>
        public float lastRoutine;
        /// <summary>
        /// Did we reach our destination
        /// </summary>
        public bool reachedDestination;

        public float lastForceNewWaypoint;

        /// <summary>
        /// 0 = Nuthin
        /// 1 = Wait for capture
        /// </summary>
        public int nextWaypointAction;
        /// <summary>
        /// Which flag are we currently capturing!
        /// </summary>
        public Kit_Domination_FlagRuntime capturingFlag;

        public Kit_BotNavPoint capturingFlagNavPoint;
        public float lastRandomAimSet;

        public Vector3 randomAimAdd;

        public float lastShot;

        public float lastEngagePlayerSet;

        public float lastEngagePosSet;

        public float burstTime;
        /// <summary>
        /// Where SHOULD we be aiming at?
        /// </summary>
        public Vector3 directAimVector;
        /// <summary>
        /// When did the bot press the reload button the last time?
        /// </summary>
        public float lastReloadTry;
    }
}