using UnityEngine;

namespace MarsFPSKit.UI
{
    /// <summary>
    /// This is really just a simple look at script that takes a camera instead of a transform, nothing special, really
    /// </summary>
    public class Kit_LoadoutLookAt : Kit_Base
    {
        public Camera camToLookAt;

        void Update()
        {
            transform.forward = Vector3.forward;
        }
    }
}