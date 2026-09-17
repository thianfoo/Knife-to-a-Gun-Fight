
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarsFPSKit
{
    /// <summary>
    /// Use this class to implement your own bot controls
    /// </summary>
    public abstract class Kit_PlayerBotControlBase : Kit_BaseScriptableObject
    {
        /// <summary>
        /// This is called for everyone when this bot is created
        /// </summary>
        /// <param name="pb"></param>
        public abstract void InitializeControls(Kit_PlayerBehaviour pb);

        /// <summary>
        /// This is the Update of the player controls. Only called on server.
        /// </summary>
        /// <param name="pb"></param>
        public abstract void WriteToPlayerInput(Kit_PlayerBehaviour pb);


        /// <summary>
        /// A relay of the OnDestroy function. Only called on server.
        /// </summary>
        /// <param name="pb"></param>
        public virtual void OnDestroyRelay(Kit_PlayerBehaviour pb) { }
    }
}