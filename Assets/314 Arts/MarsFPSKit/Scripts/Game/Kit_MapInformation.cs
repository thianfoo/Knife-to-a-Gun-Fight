using UnityEngine;
using System.Collections;

namespace MarsFPSKit
{
    /// <summary>
    /// This Object contains the complete game information (Maps, GameModes, Weapons)
    /// </summary>
    [CreateAssetMenu(menuName = "MarsFPSKit/Maps/New")]
    public class Kit_MapInformation : Kit_BaseScriptableObject
    {
        /// <summary>
        /// Name of the map as it will be displayed
        /// </summary>
        public string mapName = "Assign a map name";
        /// <summary>
        /// Actual scene name of the map
        /// </summary>
        public string sceneName = "Scene here";
        /// <summary>
        /// Image that is displayed in the voting menu
        /// </summary>
        public Sprite mapPicture;
        /// <summary>
        /// Image that is displayed when loading
        /// </summary>
        public Sprite loadingImage;
    }
}
