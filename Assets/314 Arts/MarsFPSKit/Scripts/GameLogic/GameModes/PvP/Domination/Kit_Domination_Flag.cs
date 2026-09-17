using UnityEngine;

namespace MarsFPSKit
{
    /// <summary>
    /// This represents a flag for domination game mode!
    /// </summary>
    public class Kit_Domination_Flag : Kit_Base
    {
        [Tooltip("External acceleration that will be applied to the flag's cloth")]
        /// <summary>
        /// External acceleration that will be applied to the flag's cloth
        /// </summary>
        public Vector3 externalAcceleration;
        [Tooltip("Random acceleration that will be applied to the flag's cloth")]
        /// <summary>
        /// Random acceleration that will be applied to the flag's cloth
        /// </summary>
        public Vector3 randomAcceleration;
        [Tooltip("Spawns for this flag")]
        /// <summary>
        /// Spawns for this flag
        /// </summary>
        public Kit_PlayerSpawn[] spawns;
        [Tooltip("Nav Points to capture this flag for bots")]
        /// <summary>
        /// Nav Points to capture this flag for bots
        /// </summary>
        public Kit_BotNavPoint[] botNavPoints;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            //Draw a cube to indicate
            Gizmos.DrawCube(transform.position + new Vector3(0, 1f, 0f), new Vector3(0.3f, 2f, 0.3f));
        }
    }
}
