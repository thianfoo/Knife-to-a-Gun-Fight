using MarsFPSKit.Networking;
using Mirror;
using UnityEngine;

namespace MarsFPSKit
{
    /// <summary>
    /// Mirror always needs a player object active for each connection.
    /// It also serves the purpose of telling the player his ID.
    /// </summary>
    public class Kit_PlayerPlaceholder : Kit_BaseNetworked
    {
        [SyncVar]
        /// <summary>
        /// ID of this player
        /// </summary>
        public uint id;
        public override void OnStartLocalPlayer()
        {
            //Save our ID for later use.
            networkPlayerManager.myId = id;

            //This may seem odd, but at this point we are ready and authenticated.
            if (NetworkServer.active)
            {
                if (networkManager.onHostStartedEvent != null)
                {
                    networkManager.onHostStartedEvent.Invoke();
                }
            }
        }
    }
}