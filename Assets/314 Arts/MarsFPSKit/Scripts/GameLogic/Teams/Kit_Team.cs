using UnityEngine;
using UnityEngine.Localization;

namespace MarsFPSKit
{
    [CreateAssetMenu(menuName = "MarsFPSKit/Teams/New")]
    public class Kit_Team : Kit_BaseScriptableObject
    {
        /// <summary>
        /// Name of this team
        /// </summary>
        public LocalizedString teamName;
        /// <summary>
        /// Color used for this team!
        /// </summary>
        public Color teamColor = Color.red;
        /// <summary>
        /// Image of this team
        /// </summary>
        public Sprite teamImage;
        /// <summary>
        /// ID of the player model that we have selected by default
        /// </summary>
        public int playerModelDefault;
        /// <summary>
        /// Player Models for this team
        /// </summary>
        public Kit_PlayerModelInformation[] playerModels;
    }
}