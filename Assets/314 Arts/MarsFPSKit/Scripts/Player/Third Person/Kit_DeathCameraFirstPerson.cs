using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarsFPSKit
{
    public class Kit_DeathCameraFirstPerson : Kit_DeathCameraBase
    {
        public Transform ragdollFirstPersonCamera;
        /// <summary>
        /// Was this thing set up?
        /// </summary>
        private bool wasSetup;
        /// <summary>
        /// How long until we call the game mode death cam over? Alternatively the death cam ending will also call it.
        /// </summary>
        public float timeUntilGameModeDeathCameraOverCall = 3f;
        /// <summary>
        /// Did we call the game mode?
        /// </summary>
        private bool wasGameModeCalled;
        /// <summary>
        /// Time when to call game mode
        /// </summary>
        private float callGameModeTime = float.MaxValue;

        public override void SetupDeathCamera(Kit_ThirdPersonPlayerModel model)
        {
            //This is geared towards the modern player model
            Kit_ThirdPersonModernPlayerModel modernModel = model as Kit_ThirdPersonModernPlayerModel;
            main.cameraManager.activeCameraTransform = ragdollFirstPersonCamera;

            for (int i = 0; i < modernModel.fpShadowOnlyRenderers.Length; i++)
            {
                //Make renderers visible again!
                modernModel.fpShadowOnlyRenderers[i].shadowCastingMode = ShadowCastingMode.On;
            }

            //Set time
            callGameModeTime = Time.time + timeUntilGameModeDeathCameraOverCall;
            //Enable call back
            enabled = true;
            //Set bool
            wasSetup = true;
        }

        private void OnDestroy()
        {
            if (wasSetup)
            {
                main.cameraManager.isCameraFovOverridden = false;
                if (!main.myPlayer && main.cameraManager.activeCameraTransform == ragdollFirstPersonCamera)
                {
                    main.cameraManager.activeCameraTransform = main.cameraManager.spawnCameraPosition;
                    if (!wasGameModeCalled)
                    {
                        if (main.currentPvPGameModeBehaviour)
                        {
                            //Call Game Mode
                            main.currentPvPGameModeBehaviour.OnLocalPlayerDeathCameraEnded();
                        }
                        else if (main.currentPvEGameModeBehaviour)
                        {
                            //Call Game Mode
                            main.currentPvEGameModeBehaviour.OnLocalPlayerDeathCameraEnded();
                        }
                        wasGameModeCalled = true;
                    }
                }
            }
        }
    }
}