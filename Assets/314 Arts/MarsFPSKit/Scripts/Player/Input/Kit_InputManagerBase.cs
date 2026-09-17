using UnityEngine;

namespace MarsFPSKit
{
    public abstract class Kit_InputManagerBase : Kit_BaseScriptableObject
    {
        /// <summary>
        /// This is called for everyone when this bot is created
        /// </summary>
        /// <param name="pb"></param>
        public abstract void InitializeServer(Kit_PlayerBehaviour pb);

        /// <summary>
        /// This is called for everyone when this bot is created
        /// </summary>
        /// <param name="pb"></param>
        public abstract void InitializeClient(Kit_PlayerBehaviour pb);

        /// <summary>
        /// This is the Update of the player controls
        /// </summary>
        /// <param name="pb"></param>
        public abstract void WriteToPlayerInput(Kit_PlayerBehaviour pb);

        /// <summary>
        /// Called at certain points when aiming should be reset
        /// </summary>
        /// <param name="pb"></param>
        public abstract void ResetAiming(Kit_PlayerBehaviour pb);
    }
}
