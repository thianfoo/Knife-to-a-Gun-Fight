using Mirror;
using UnityEngine;

namespace MarsFPSKit.Weapons
{
    public abstract class Kit_AttachmentSyncDataBase : Kit_BaseNetworked
    {
        /// <summary>
        /// Slot of this attachment
        /// </summary>
        [SyncVar]
        public int slot;
    }
}