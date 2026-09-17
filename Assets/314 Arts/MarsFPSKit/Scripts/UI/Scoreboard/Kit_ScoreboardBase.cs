using UnityEngine;

namespace MarsFPSKit.UI
{
    public abstract class Kit_ScoreboardBase : Kit_Base
    {
        /// <summary>
        /// Is the scoreboard currently open?
        /// </summary>
        public bool isOpen;

        /// <summary>
        /// Enables the use of the scoreboard
        /// </summary>
        public abstract void Enable();

        /// <summary>
        /// Disables the use of the scoreboard
        /// </summary>
        public abstract void Disable();
    }
}