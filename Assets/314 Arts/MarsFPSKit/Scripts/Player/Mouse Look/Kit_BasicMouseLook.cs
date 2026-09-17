using Mirror;
using UnityEngine;

namespace MarsFPSKit
{
    /// <summary>
    /// Implements a basic Mouse Look on the X and Y axis
    /// </summary>
    [CreateAssetMenu(menuName = "MarsFPSKit/Looking/Basic Mouse Look")]
    public class Kit_BasicMouseLook : Kit_MouseLookBase
    {
        [Header("Basic Sensitivity")]
        [Tooltip("Input multiplier for x rotation")]
        public float basicSensitivityX = 1f;

        [Tooltip("Input multiplier for y rotation")]
        public float basicSensitivityY = 1f;

        [Header("Limits")]
        public float minY = -85f; // Minimum for y looking
        public float maxY = 85f;  // Maximum for y looking

        [Header("Leaning")]
        public bool leaningEnabled;

        public Vector3 leftLeanPos;
        public Vector3 leftLeanRot;
        public Vector3 leftLeanWepPos;
        public Vector3 leftLeanWepRot;

        public Vector3 rightLeanPos;
        public Vector3 rightLeanRot;
        public Vector3 rightLeanWepPos;
        public Vector3 rightLeanWepRot;

        public float leaningSpeedMultiplier = 0.7f;
        public float leaningRotationSmoothSpeed = 15f;

        /// <summary>
        /// How fast is the leaning state going to change
        /// </summary>
        public float leaningSpeed = 2f;

        [Header("Perspective")]
        public Vector3 cameraFirstPersonPosition;
        public Vector3 cameraThirdPersonPosition;

        /// <summary>
        /// How fast is the transition from first to third person (and vice versa)?
        /// </summary>
        public float firstThirdPersonChangeSpeed = 4f;

        [Header("Clipping Avoidance")]
        public bool enableCameraClippingAvoidment = true; // kept name for compatibility
        public LayerMask clippingAvoidmentMask;
        public float clippingAvoidmentCorrection = 0.01f;

        [Header("World Position Crosshair")]
        public bool enableWorldPositionCrosshair = true;
        public LayerMask worldPositionCrosshairMask;
        public float worldPositionCrosshairMaxDistance = 100f;

        public override void InitializeServer(Kit_PlayerBehaviour pb)
        {
            GameObject dataObj = Instantiate(networkData, Vector3.zero, Quaternion.identity);
            var nData = dataObj.GetComponent<Kit_BasicMouseLookNetworkData>();

            pb.customMouseLookData = nData;
            nData.ownerPlayerNetworkId = pb.netId;

            if (pb.isBot)
                NetworkServer.Spawn(dataObj);
            else
                NetworkServer.Spawn(dataObj, pb.gameObject);
        }

        public override void InitializeClient(Kit_PlayerBehaviour pb)
        {
            var data = pb.customMouseLookData as Kit_BasicMouseLookNetworkData;
            if (data == null) return;

            // Set initial look
            if (pb.input != null)
                pb.input.mouseX = pb.transform.localEulerAngles.y;

            var gi = main.gameInformation;

            bool defaultFirst =
                gi.perspectiveMode == Kit_GameInformation.PerspectiveMode.FirstPersonOnly ||
                (gi.perspectiveMode == Kit_GameInformation.PerspectiveMode.Both &&
                 gi.defaultPerspective == Kit_GameInformation.Perspective.FirstPerson);

            bool defaultThird =
                gi.perspectiveMode == Kit_GameInformation.PerspectiveMode.ThirdPersonOnly ||
                (gi.perspectiveMode == Kit_GameInformation.PerspectiveMode.Both &&
                 gi.defaultPerspective == Kit_GameInformation.Perspective.ThirdPerson);

            if (defaultFirst)
            {
                data.desiredPerspective = Kit_GameInformation.Perspective.FirstPerson;
                data.currentPerspective = Kit_GameInformation.Perspective.FirstPerson;
                data.firstPersonThirdPersonBlend = 0f;
            }
            else if (defaultThird)
            {
                data.desiredPerspective = Kit_GameInformation.Perspective.ThirdPerson;
                data.currentPerspective = Kit_GameInformation.Perspective.ThirdPerson;
                data.firstPersonThirdPersonBlend = 1f;
            }
        }

        public override void TakeControl(Kit_PlayerBehaviour pb)
        {
            var data = pb.customMouseLookData as Kit_BasicMouseLookNetworkData;
            if (data == null) return;

            if (pb.isFirstPersonActive)
                UpdatePerspectiveScripts(pb, data.currentPerspective);
        }

        public override void AuthorativeInput(Kit_PlayerBehaviour pb, Kit_PlayerInput input, float delta, double revertTime)
        {
            var data = pb.customMouseLookData as Kit_BasicMouseLookNetworkData;
            if (data == null) return;

            // Special case: Listen-server host smoothing (kept behavior)
            bool smoothHostCase = (!pb.isBot && !pb.isLocalPlayer);

            ProcessLookPipeline(pb, data, input, delta, smoothHostCase);
        }

        public override void PredictionInput(Kit_PlayerBehaviour pb, Kit_PlayerInput input, float delta)
        {
            var data = pb.customMouseLookData as Kit_BasicMouseLookNetworkData;
            if (data == null) return;

            ProcessLookPipeline(pb, data, input, delta, smoothHostCase: false);
        }

        private void ProcessLookPipeline(Kit_PlayerBehaviour pb, Kit_BasicMouseLookNetworkData data, Kit_PlayerInput input, float dt, bool smoothHostCase)
        {
            // 1) Copy input (input is the source of truth here)
            data.mouseY = input.mouseY;
            data.mouseX = input.mouseX;

            // 2) Leaning
            UpdateLeaning(pb, data, input, dt);

            // 3) Recoil + clamps
            ApplyRecoilAndClamp(pb, data);

            // 4) Apply transforms
            ApplyRotations(pb, data, dt, smoothHostCase);

            // 5) Perspective toggle
            HandlePerspectiveToggle(pb, data, input);
        }

        private void UpdateLeaning(Kit_PlayerBehaviour pb, Kit_BasicMouseLookNetworkData data, Kit_PlayerInput input, float dt)
        {
            if (!leaningEnabled)
            {
                data.leaningState = 0f;
                return;
            }

            float target = 0f;

            if (!pb.movement.IsRunning(pb))
            {
                if (input.leanLeft) target = -1f;
                else if (input.leanRight) target = 1f;
            }

            data.leaningState = Mathf.MoveTowards(data.leaningState, target, dt * leaningSpeed);
        }

        private void ApplyRecoilAndClamp(Kit_PlayerBehaviour pb, Kit_BasicMouseLookNetworkData data)
        {
            // Convert recoil Euler angles to signed degrees safely
            float recoilPitch = Mathf.DeltaAngle(0f, pb.recoilApplyRotation.eulerAngles.x);
            float recoilYaw = Mathf.DeltaAngle(0f, pb.recoilApplyRotation.eulerAngles.y);

            data.recoilMouseY = recoilPitch;
            data.recoilMouseX = -recoilYaw;

            // Prevent recoil pushing past maxY (keeps same intent as original)
            float pitchWithRecoil = data.mouseY + data.recoilMouseY;
            if (pitchWithRecoil > maxY)
                data.mouseY -= (pitchWithRecoil - maxY);

            // Clamp input pitch
            data.mouseY = Mathf.Clamp(data.mouseY, minY, maxY);

            // Apply recoil pitch
            data.finalMouseY = Mathf.Clamp(data.mouseY + data.recoilMouseY, minY, maxY);

            // Wrap yaw to [0..360)
            data.mouseX = Mathf.Repeat(data.mouseX, 360f);

            // Apply recoil yaw
            data.finalMouseX = data.mouseX + data.recoilMouseX;
        }

        private void ApplyRotations(Kit_PlayerBehaviour pb, Kit_BasicMouseLookNetworkData data, float dt, bool smoothHostCase)
        {
            // Bots: handled differently
            if (pb.isBot)
            {
                data.finalMouseY = -pb.mouseLookObject.localEulerAngles.x;
                if (data.finalMouseY < -180f) data.finalMouseY += 360f;
                data.mouseY = data.finalMouseY;
                return;
            }

            // Authoritative host smoothing case: only rotate here to keep feel (kept behavior)
            if (smoothHostCase)
            {
                pb.transform.rotation = Quaternion.Euler(0f, data.finalMouseX, 0f);

                data.leaningSmoothState = Quaternion.Slerp(
                    data.leaningSmoothState,
                    GetCameraRotationOffset(pb),
                    dt * leaningRotationSmoothSpeed
                );

                pb.mouseLookObject.localRotation = Quaternion.Euler(-data.finalMouseY, 0f, 0f) * data.leaningSmoothState;
                return;
            }

            // Prediction (and normal) rotation path
            pb.transform.rotation = Quaternion.Euler(0f, data.finalMouseX, 0f);

            data.leaningSmoothState = Quaternion.Slerp(
                data.leaningSmoothState,
                GetCameraRotationOffset(pb),
                dt * leaningRotationSmoothSpeed
            );

            pb.mouseLookObject.localRotation = Quaternion.Euler(-data.finalMouseY, 0f, 0f) * data.leaningSmoothState;
        }

        private void HandlePerspectiveToggle(Kit_PlayerBehaviour pb, Kit_BasicMouseLookNetworkData data, Kit_PlayerInput input)
        {
            if (main.gameInformation.perspectiveMode != Kit_GameInformation.PerspectiveMode.Both)
                return;

            if (data.lastThirdPersonButton == input.thirdPerson)
                return;

            data.lastThirdPersonButton = input.thirdPerson;

            if (!input.thirdPerson)
                return;

            data.desiredPerspective =
                (data.desiredPerspective == Kit_GameInformation.Perspective.FirstPerson)
                    ? Kit_GameInformation.Perspective.ThirdPerson
                    : Kit_GameInformation.Perspective.FirstPerson;
        }

        public override void NotControllerUpdate(Kit_PlayerBehaviour pb)
        {
            // intentionally empty
        }

        public override void Visuals(Kit_PlayerBehaviour pb)
        {
            var data = pb.customMouseLookData as Kit_BasicMouseLookNetworkData;
            if (data == null) return;

            // --- Perspective Blend ---
            bool forcedFirstPersonByAim =
                (main.gameInformation.thirdPersonAiming == Kit_GameInformation.ThirdPersonAiming.GoIntoFirstPerson && pb.weaponManager.IsAiming(pb))
                || pb.weaponManager.ForceIntoFirstPerson(pb);

            if (forcedFirstPersonByAim)
            {
                // Use aim-in time for smoother transition while aiming
                float aimTime = Mathf.Max(0.0001f, pb.weaponManager.AimInTime(pb));
                data.firstPersonThirdPersonBlend -= Time.deltaTime / aimTime;
            }
            else if (data.desiredPerspective == Kit_GameInformation.Perspective.FirstPerson)
            {
                data.firstPersonThirdPersonBlend -= Time.deltaTime * firstThirdPersonChangeSpeed;
            }
            else
            {
                data.firstPersonThirdPersonBlend += Time.deltaTime * firstThirdPersonChangeSpeed;
            }

            data.firstPersonThirdPersonBlend = Mathf.Clamp01(data.firstPersonThirdPersonBlend);

            // Decide active perspective based on blend
            if (Mathf.Approximately(data.firstPersonThirdPersonBlend, 0f) && data.currentPerspective != Kit_GameInformation.Perspective.FirstPerson)
            {
                data.currentPerspective = Kit_GameInformation.Perspective.FirstPerson;
                UpdatePerspectiveScripts(pb, Kit_GameInformation.Perspective.FirstPerson);
            }
            else if (!Mathf.Approximately(data.firstPersonThirdPersonBlend, 0f) && data.currentPerspective != Kit_GameInformation.Perspective.ThirdPerson)
            {
                data.currentPerspective = Kit_GameInformation.Perspective.ThirdPerson;
                UpdatePerspectiveScripts(pb, Kit_GameInformation.Perspective.ThirdPerson);
            }

            // --- Camera positioning / clipping avoidance ---
            if (main.cameraManager.activeCameraTransform == pb.playerCameraTransform)
            {
                Transform cam = main.cameraManager.mainCamera.transform;
                Transform camParent = cam.parent;

                Vector3 desiredLocal = Vector3.Lerp(cameraFirstPersonPosition, cameraThirdPersonPosition, data.firstPersonThirdPersonBlend);

                if (enableCameraClippingAvoidment && data.currentPerspective == Kit_GameInformation.Perspective.ThirdPerson)
                {
                    Vector3 from = pb.playerCameraTransform.position;
                    Vector3 toWorld = pb.playerCameraTransform.TransformPoint(cameraThirdPersonPosition);

                    if (Physics.Linecast(from, toWorld, out data.perspectiveClippingAvoidmentHit, clippingAvoidmentMask, QueryTriggerInteraction.Ignore))
                    {
                        Vector3 correctedWorld = data.perspectiveClippingAvoidmentHit.point +
                                                 data.perspectiveClippingAvoidmentHit.normal * clippingAvoidmentCorrection;

                        Vector3 correctedLocal = camParent.InverseTransformPoint(correctedWorld);
                        desiredLocal = Vector3.Lerp(cameraFirstPersonPosition, correctedLocal, data.firstPersonThirdPersonBlend);
                    }
                }

                cam.localPosition = desiredLocal;
            }

            // --- World-position crosshair (3rd person only) ---
            if (enableWorldPositionCrosshair)
            {
                if (data.currentPerspective == Kit_GameInformation.Perspective.ThirdPerson)
                {
                    Vector3 origin = pb.playerCameraTransform.position;
                    Vector3 direction = pb.playerCameraTransform.forward;

                    Vector3 targetWorld;
                    if (Physics.Raycast(origin, direction, out data.worldPositionCrosshair, worldPositionCrosshairMaxDistance, worldPositionCrosshairMask.value))
                        targetWorld = data.worldPositionCrosshair.point;
                    else
                        targetWorld = origin + direction * 100f;

                    Vector3 canvasPos = main.canvas.WorldToCanvas(targetWorld, main.cameraManager.mainCamera);
                    main.hud.MoveCrosshairTo(canvasPos);
                }
                else
                {
                    main.hud.MoveCrosshairTo(Vector3.zero);
                }
            }
        }

        private void UpdatePerspectiveScripts(Kit_PlayerBehaviour pb, Kit_GameInformation.Perspective perspective)
        {
            if (!pb.isFirstPersonActive) return;

            pb.weaponManager.FirstThirdPersonChanged(pb, perspective);
            pb.thirdPersonPlayerModel.FirstThirdPersonChanged(pb, perspective);
        }

        public override bool ReachedYMax(Kit_PlayerBehaviour pb)
        {
            if (pb.customMouseLookData is not Kit_BasicMouseLookNetworkData data)
                return false;

            return Mathf.Approximately(data.finalMouseY, minY) || Mathf.Approximately(data.finalMouseY, maxY);
        }

        public override float GetSpeedMultiplier(Kit_PlayerBehaviour pb)
        {
            if (pb.customMouseLookData is not Kit_BasicMouseLookNetworkData data)
                return 1f;

            return Mathf.Lerp(1f, leaningSpeedMultiplier, Mathf.Abs(data.leaningState));
        }

        private static void GetLeanBlend(float lean, out bool right, out float t)
        {
            right = lean > 0f;
            t = Mathf.Abs(lean);
        }

        public override Vector3 GetCameraOffset(Kit_PlayerBehaviour pb)
        {
            if (pb.customMouseLookData is not Kit_BasicMouseLookNetworkData data)
                return base.GetCameraOffset(pb);

            GetLeanBlend(data.leaningState, out bool right, out float t);
            return Vector3.Lerp(Vector3.zero, right ? rightLeanPos : leftLeanPos, t);
        }

        public override Quaternion GetCameraRotationOffset(Kit_PlayerBehaviour pb)
        {
            if (pb.customMouseLookData is not Kit_BasicMouseLookNetworkData data)
                return base.GetCameraRotationOffset(pb);

            GetLeanBlend(data.leaningState, out bool right, out float t);
            return Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(right ? rightLeanRot : leftLeanRot), t);
        }

        public override Vector3 GetWeaponOffset(Kit_PlayerBehaviour pb)
        {
            if (pb.weaponManager.IsAiming(pb))
                return Vector3.zero;

            if (pb.customMouseLookData is not Kit_BasicMouseLookNetworkData data)
                return base.GetWeaponOffset(pb);

            GetLeanBlend(data.leaningState, out bool right, out float t);
            return Vector3.Lerp(Vector3.zero, right ? rightLeanWepPos : leftLeanWepPos, t);
        }

        public override Quaternion GetWeaponRotationOffset(Kit_PlayerBehaviour pb)
        {
            if (pb.weaponManager.IsAiming(pb))
                return Quaternion.identity;

            if (pb.customMouseLookData is not Kit_BasicMouseLookNetworkData data)
                return base.GetWeaponRotationOffset(pb);

            GetLeanBlend(data.leaningState, out bool right, out float t);
            return Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(right ? rightLeanWepRot : leftLeanWepRot), t);
        }

        public override Kit_GameInformation.Perspective GetPerspective(Kit_PlayerBehaviour pb)
        {
            if (!pb.isFirstPersonActive)
                return Kit_GameInformation.Perspective.ThirdPerson;

            if (pb.customMouseLookData is Kit_BasicMouseLookNetworkData data)
                return data.currentPerspective;

            return Kit_GameInformation.Perspective.FirstPerson;
        }
    }
}
