
using MarsFPSKit.Networking;
using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace MarsFPSKit
{
    [CreateAssetMenu(menuName = "MarsFPSKit/Ping Limiter/Basic Ping Limiter")]
    /// <summary>
    /// Implements a basic ping limiter that will display warnings and disconnects if the warnings have been ignored or the user was unable to improve his ping.
    /// </summary>
    public class Kit_PingLimiterSimple : Kit_PingLimitBase
    {
        [Tooltip("How many times is the user warned before he is kicked?")]
        /// <summary>
        /// How many times is the user warned before he is kicked?
        /// </summary>
        public int amountOfWarnings = 3;

        [Tooltip("How many seconds apart is the ping checked?")]
        /// <summary>
        /// How many seconds apart is the ping checked?
        /// </summary>
        public float pingCheckInterval = 10f;

        //RUNTIME DATA
        /// <summary>
        /// How many times has the user been warned in a row?
        /// </summary>
        private Dictionary<uint, ushort> currentNumberOfWarnings = new Dictionary<uint, ushort>();

        /// <summary>
        /// When was the ping checked for the last time?
        /// </summary>
        private float lastPingCheck;

        /// <summary>
        /// What is the current ping limit?
        /// </summary>
        private ushort currentPingLimit;
        //END

        public override void StartRelay(bool enabled, ushort pingLimit = 0)
        {
            //Reset runtime data
            lastPingCheck = Time.time;
            currentNumberOfWarnings.Clear();
            currentPingLimit = pingLimit;
        }

        public override void UpdateRelay()
        {
            //Check if we need to check the ping
            if (Time.time - pingCheckInterval > lastPingCheck)
            {
                for (int i = 0; i < networkPlayerManager.players.Count; i++)
                {
                    if (!currentNumberOfWarnings.ContainsKey(networkPlayerManager.players[i].id))
                    {
                        currentNumberOfWarnings.Add(networkPlayerManager.players[i].id, 0 );
                    }

                    //Check if our ping is too high
                    if (networkPlayerManager.players[i].ping >= currentPingLimit)
                    {
                        currentNumberOfWarnings[networkPlayerManager.players[i].id]++;
                        if (currentNumberOfWarnings[networkPlayerManager.players[i].id] > amountOfWarnings)
                        {
                            networkPlayerManager.players[i].serverToClientConnection.Disconnect();
                        }
                        else
                        {
                            main.TargetDisplayPingWarning(networkPlayerManager.players[i].serverToClientConnection, networkPlayerManager.players[i].ping, currentNumberOfWarnings[networkPlayerManager.players[i].id]);
                        }
                    }
                    else
                    {
                        currentNumberOfWarnings[networkPlayerManager.players[i].id] = 0;
                    }
                }

                //Check last ping check
                lastPingCheck = Time.time;
            }
        }
    }
}
