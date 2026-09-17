using Mirror;
using UnityEngine;

namespace MarsFPSKit
{
    public abstract class Kit_MovementNetworkBase : Kit_BaseNetworked
    {
        [SyncVar]
        /// <summary>
        /// The network id of the player object that this belongs to
        /// </summary>
        public uint ownerPlayerNetworkId;

        public override void OnStartServer()
        {
            if (NetworkServer.spawned.TryGetValue(ownerPlayerNetworkId, out var playerNid))
            {
                Kit_PlayerBehaviour pb = playerNid.GetComponent<Kit_PlayerBehaviour>();
                pb.movementNetworkData = this;
            }
        }

        public override void OnStartClient()
        {
            if (NetworkClient.spawned.TryGetValue(ownerPlayerNetworkId, out var playerNid))
            {
                Kit_PlayerBehaviour pb = playerNid.GetComponent<Kit_PlayerBehaviour>();
                pb.movementNetworkData = this;

                //Initialize movement
                pb.movement.InitializeClient(pb);
            }
        }
    }
}