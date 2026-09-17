using UnityEngine;

namespace MarsFPSKit
{
    [DefaultExecutionOrder(10)]
    public class Kit_CameraManager : Kit_Base
    {
        //This contains the local camera control
        #region Camera Control
        [Header("Camera Control")]
        public Camera mainCamera; //The main camera to use for the whole game
#if INTEGRATION_FPV2
        public Camera weaponCamera; //This is a second camera used by FPV2 for the matrix.
#elif INTEGRATION_FPV3
        public Camera weaponCamera; //This is a second camera used by FPV2 for the matrix.
#endif
        /// <summary>
        /// Camera shake!
        /// </summary>
        public Kit_CameraShake cameraShake;
        //We recycle the same camera for the whole game, for easy setup of image effects
        //Be careful when changing near and far clip
        public Transform activeCameraTransform
        {
            get
            {
                return _activeCameraTransform;
            }
            set
            {
                if (value)
                {
                    DebugLog("[Camera] Changing camera parent to " + value.name, value);
                }
                else
                {
                    DebugLog("[Camera] Changing camera parent to null");
                }

                _activeCameraTransform = value;
            }
        }

        private Transform _activeCameraTransform;

        public Transform spawnCameraPosition; //The spawn position for the camera
        #endregion

        [HideInInspector]
        /// <summary>
        /// Is the camera fov overriden?
        /// </summary>
        public bool isCameraFovOverridden;


        public void OnEnable()
        {
            //Check if we shall replace camera
            if (main.gameInformation.mainCameraOverride)
            {
                //Instantiate new
                GameObject newCamera = Instantiate(main.gameInformation.mainCameraOverride, mainCamera.transform, false);
                //Reparent
                newCamera.transform.parent = spawnCameraPosition;
                //Destroy camera
                Destroy(mainCamera.gameObject);
                //Assign new camera
                mainCamera = newCamera.GetComponent<Camera>();
                //Camera Shake
                cameraShake = newCamera.GetComponentInChildren<Kit_CameraShake>();
#if INTEGRATION_FPV2
                //Get weapon camera
                weaponCamera = newCamera.GetComponentInChildren<FirstPersonView.ShaderMaterialSolution.FPV_SM_FirstPersonCamera>().GetComponent<Camera>();
                if (!weaponCamera) DebugLogError("FPV2 is enabled but correct prefab is not assigned!", main.gameInformation.mainCameraOverride);
#elif INTEGRATION_FPV3
                //Get weapon camera
                weaponCamera = newCamera.GetComponentInChildren<FirstPersonView.FPV_Camera_FirstPerson>().GetComponent<Camera>();
                if (!weaponCamera) DebugLogError("FPV3 is enabled but correct prefab is not assigned!", main.gameInformation.mainCameraOverride);
#endif
            }
        }

        private void Start()
        {
            //Make sure the main camera is child of the spawn camera position
            activeCameraTransform = spawnCameraPosition;
        }

        private void Update()
        {
            #region FOV
            if (!main.myPlayer && (!main.spectatorManager || !main.spectatorManager.IsCurrentlySpectating()))
            {
                if (!isCameraFovOverridden)
                    mainCamera.fieldOfView = Kit_GameSettings.baseFov;
            }
#if INTEGRATION_FPV2
                else
                {
                    if (weaponCamera)
                    {
                        //Make sure FOV is the same.
                        weaponCamera.fieldOfView = mainCamera.fieldOfView;
                    }
                }
#elif INTEGRATION_FPV3
                else
                {
                    if (weaponCamera)
                    {
                        //Make sure FOV is the same.
                        weaponCamera.fieldOfView = mainCamera.fieldOfView;
                    }
                }
#endif
            #endregion
        }

        private void LateUpdate()
        {
            if (_activeCameraTransform)
                mainCamera.transform.SetPositionAndRotation(_activeCameraTransform.position, _activeCameraTransform.rotation);
        }
    }
}