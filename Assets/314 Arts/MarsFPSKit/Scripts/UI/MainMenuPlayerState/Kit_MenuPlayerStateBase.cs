using UnityEngine;

namespace MarsFPSKit.UI
{
    /// <summary>
    /// Use this to implement your own menu player state
    /// </summary>
    public abstract class Kit_MenuPlayerStateBase : Kit_Base
    {
        /// <summary>
        /// Called once the player is logged in
        /// </summary>
        /// <param name="main"></param>
        public abstract void Initialize(Kit_MenuManager main);
    }
}