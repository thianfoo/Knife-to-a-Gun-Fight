using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

namespace MarsFPSKit
{
    public class Kit_DeathCameraThirdPerson : Kit_DeathCameraBase
    {
        /// <summary>
        /// Where to look at
        /// </summary>
        public Transform lookAtTransform;
        /// <summary>
        /// How quickly does the camera react
        /// </summary>
        public float lookAtSmooth = 5f;
        /// <summary>
        /// Reference distance for FOV
        /// </summary>
        public float distanceFovReference = 30f;
        /// <summary>
        /// Smallest FOV the camera will fade to
        /// </summary>
        public float smallestFov = 40f;
        /// <summary>
        /// How long until we call the game mode death cam over? Alternatively the death cam ending will also call it.
        /// </summary>
        public float timeUntilGameModeDeathCameraOverCall = 3f;
        /// <summary>
        /// Did we call the game mode?
        /// </summary>
        private bool wasGameModeCalled;
        /// <summary>
        /// Where we died
        /// </summary>
        private Vector3 deathPos;
        /// <summary>
        /// Was this thing set up?
        /// </summary>
        private bool wasSetup;
        /// <summary>
        /// Time when to call game mode
        /// </summary>
        private float callGameModeTime = float.MaxValue;

        private void Update()
        {
            if (lookAtTransform && !main.myPlayer && main.cameraManager.activeCameraTransform == null)
            {
                main.cameraManager.mainCamera.transform.position = deathPos;
                //main.mainCamera.transform.forward = Vector3.Slerp(main.mainCamera.transform.forward, lookAtTransform.position - main.mainCamera.transform.position, Time.deltaTime * lookAtSmooth);
                main.cameraManager.mainCamera.transform.rotation = Quaternion.Slerp(main.cameraManager.mainCamera.transform.rotation, Quaternion.LookRotation(lookAtTransform.position - main.cameraManager.mainCamera.transform.position), Time.deltaTime * lookAtSmooth);

                main.cameraManager.mainCamera.fieldOfView = Mathf.Lerp(main.cameraManager.mainCamera.fieldOfView, Mathf.Lerp(Kit_GameSettings.baseFov, smallestFov, Vector3.Distance(main.cameraManager.mainCamera.transform.position, lookAtTransform.position) / distanceFovReference), Time.deltaTime * lookAtSmooth);
            }

            if (main.myPlayer)
            {
                enabled = false;
                main.cameraManager.isCameraFovOverridden = false;
                wasSetup = false;
            }

            if (!wasGameModeCalled && wasSetup)
            {
                if (Time.time > callGameModeTime)
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

        public override void SetupDeathCamera(Kit_ThirdPersonPlayerModel model)
        {
            //This is geared towards the modern player model
            Kit_ThirdPersonModernPlayerModel modernModel = model as Kit_ThirdPersonModernPlayerModel;
            main.cameraManager.activeCameraTransform = null;
            main.cameraManager.isCameraFovOverridden = true;
            deathPos = modernModel.kpb.playerCameraTransform.position;
            main.cameraManager.mainCamera.transform.position = modernModel.kpb.playerCameraTransform.position;
            main.cameraManager.mainCamera.transform.rotation = modernModel.kpb.playerCameraTransform.rotation;
            //Show
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
            if (wasSetup && !sceneSyncer.isLoading)
            {
                main.cameraManager.isCameraFovOverridden = false;
                if (!main.myPlayer && main.cameraManager.activeCameraTransform == null)
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