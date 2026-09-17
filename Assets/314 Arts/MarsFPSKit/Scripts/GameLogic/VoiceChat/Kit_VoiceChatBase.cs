using UnityEngine;

namespace MarsFPSKit
{
    /// <summary>
    /// Use this to implement your own voice chat
    /// </summary>
    public abstract class Kit_VoiceChatBase : Kit_Base
    {
        public abstract void Setup();

        /// <summary>
        /// Called when the player has joined a team or switched
        /// </summary>
        /// <param name="team"></param>
        public abstract void JoinedTeam(int team);
    }
}
