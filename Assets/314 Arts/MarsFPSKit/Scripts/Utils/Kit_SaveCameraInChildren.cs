using UnityEngine;

namespace MarsFPSKit
{
    public class Kit_SaveCameraInChildren : Kit_Base
    {
        public void OnDestroy()
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam)
            {
                if (cam == main.cameraManager.mainCamera)
                {
                    main.cameraManager.activeCameraTransform = main.cameraManager.spawnCameraPosition;
                }
            }
        }
    }
}