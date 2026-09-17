using Mirror;
using UnityEngine;

namespace MarsFPSKit
{
    public abstract class Kit_VitalsNetworkBase : Kit_BaseNetworked
    {
        [SyncVar]
        /// <summary>
        /// The network id of the player object that this belongs to
        /// </summary>
        public uint ownerPlayerNetworkId;
        /// <summary>
        /// Our player component
        /// </summary>
        public Kit_PlayerBehaviour pb;

        public override void OnStartServer()
        {
            if (NetworkServer.spawned.TryGetValue(ownerPlayerNetworkId, out var playerNid))
            {
                pb = playerNid.GetComponent<Kit_PlayerBehaviour>();
                pb.vitalsNetworkData = this;
            }
        }

        public override void OnStartClient()
        {
            if (NetworkClient.spawned.TryGetValue(ownerPlayerNetworkId, out var playerNid))
            {
                pb = playerNid.GetComponent<Kit_PlayerBehaviour>();
                pb.vitalsNetworkData = this;
            }
        }
    }
}