using UnityEngine;

namespace MarsFPSKit
{
    public abstract class Kit_BotLoadoutManager : Kit_BaseScriptableObject
    {
        /// <summary>
        /// Gets a loadout for a bot
        /// </summary>
        /// <param name="main"></param>
        /// <returns></returns>
        public abstract Loadout GetBotLoadout();
    }
}
