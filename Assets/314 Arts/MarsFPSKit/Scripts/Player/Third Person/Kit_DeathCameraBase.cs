using UnityEngine;

namespace MarsFPSKit
{
    public abstract class Kit_DeathCameraBase : Kit_Base
    {
        public abstract void SetupDeathCamera(Kit_ThirdPersonPlayerModel model);
    }
}