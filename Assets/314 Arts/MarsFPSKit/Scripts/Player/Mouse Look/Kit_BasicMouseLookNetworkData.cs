using Mirror;
using UnityEngine;

namespace MarsFPSKit
{
    public class Kit_BasicMouseLookNetworkData : Kit_MouseLookNetworkBase
    {
        // -------------------------
        // Simulated / input state
        // -------------------------

        /// <summary>Rotation on Unity Y-Axis (Player Object)</summary>
        public float mouseX;

        /// <summary>Rotation on Unity X-Axis (Camera/Weapons)</summary>
        public float mouseY;

        /// <summary>Recoil on x axis (yaw)</summary>
        public float recoilMouseX;

        /// <summary>Recoil on y axis (pitch)</summary>
        public float recoilMouseY;

        /// <summary>Rotation on Unity Y-Axis with recoil applied</summary>
        public float finalMouseX;

        /// <summary>Rotation on Unity X-Axis with recoil applied</summary>
        [SyncVar]
        public float finalMouseY;

        // -------------------------
        // Leaning
        // -------------------------

        /// <summary>How are we currently leaning -1 (L) to 1 (R)</summary>
        [SyncVar]
        public float leaningState;

        /// <summary>Smoothed lean rotation (local-only, not synced)</summary>
        public Quaternion leaningSmoothState = Quaternion.identity;

        // -------------------------
        // Perspective state
        // -------------------------

        /// <summary>Last input of the third person button (local-only)</summary>
        public bool lastThirdPersonButton;

        /// <summary>Were we aiming last? (local-only)</summary>
        public bool wasAimingLast;

        /// <summary>
        /// 0 = First person pos, 1 = Third person pos (local visual blend)
        /// </summary>
        public float firstPersonThirdPersonBlend;

        /// <summary>The currently desired perspective</summary>
        public Kit_GameInformation.Perspective desiredPerspective = Kit_GameInformation.Perspective.FirstPerson;

        /// <summary>The currently active perspective</summary>
        public Kit_GameInformation.Perspective currentPerspective = Kit_GameInformation.Perspective.FirstPerson;

        // -------------------------
        // Visual caches (local-only)
        // -------------------------

        /// <summary>Hit cache for clipping avoidance (visual-only)</summary>
        public RaycastHit perspectiveClippingAvoidmentHit;

        /// <summary>Hit cache for crosshair (visual-only)</summary>
        public RaycastHit worldPositionCrosshair;
    }
}