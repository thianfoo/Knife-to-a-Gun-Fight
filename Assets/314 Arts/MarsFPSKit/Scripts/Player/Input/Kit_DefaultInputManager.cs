using UnityEngine;

namespace MarsFPSKit
{
    public class DefaultInputData
    {
        public bool resetAim;


        public float ver, hor;
    }

    [CreateAssetMenu(menuName = "MarsFPSKit/Input Manager/Default")]
    /// <summary>
    /// This is the kit's default input manager
    /// </summary>
    public class Kit_DefaultInputManager : Kit_InputManagerBase
    {
        public override void InitializeServer(Kit_PlayerBehaviour pb)
        {
            DefaultInputData did = new DefaultInputData();
            pb.inputManagerData = did;
            pb.input.weaponSlotUses = new bool[inputSystem.weaponSelectButtons.Length];
            pb.inputCache.weaponSlotUses = new bool[inputSystem.weaponSelectButtons.Length];
        }

        public override void InitializeClient(Kit_PlayerBehaviour pb)
        {
            DefaultInputData did = new DefaultInputData();
            pb.inputManagerData = did;
            pb.inputCache.weaponSlotUses = new bool[inputSystem.weaponSelectButtons.Length];
            pb.input.weaponSlotUses = new bool[inputSystem.weaponSelectButtons.Length];
        }

        public override void WriteToPlayerInput(Kit_PlayerBehaviour pb)
        {
            if (pb == null) return;
            if (pb.inputManagerData == null) return;
            if (pb.inputManagerData is not DefaultInputData did) return;

            if (pb.enableInput)
            {
                //Get all input
                did.hor = Mathf.Lerp(did.hor, inputSystem.movementHorizontal, Time.deltaTime * 20);
                did.ver = Mathf.Lerp(did.ver, inputSystem.movementVertical, Time.deltaTime * 20);

                pb.inputCache.hor = (float)System.Math.Round(did.hor, 1);
                pb.inputCache.ver = (float)System.Math.Round(did.ver, 1);
                pb.inputCache.crouch = inputSystem.crouchHold;
                pb.inputCache.sprint = inputSystem.runHold;
                pb.inputCache.jump = inputSystem.jump;
                pb.inputCache.interact = inputSystem.useHold;

                //Hold
                if (!Kit_GameSettings.isAimingToggle)
                {
                    pb.inputCache.aiming = inputSystem.aimHold;
                }
                //Toggle
                else
                {
                    if (inputSystem.aimPress)
                    {
                        pb.inputCache.aiming = !pb.inputCache.aiming;
                        DebugLog("Toggling aim");
                    }
                }

                if (did.resetAim)
                {
                    pb.inputCache.aiming = false;
                    did.resetAim = false;
                }

                pb.inputCache.rmb = inputSystem.aimHold;
                pb.inputCache.reload = inputSystem.reloadHold;

                float sens = pb.weaponManager.CurrentSensitivity(pb);

                //Changed  from pun to mirror: Mouse X and Mouse Y is now the desired look rotation
                pb.inputCache.mouseX += inputSystem.lookX * sens;
                pb.inputCache.mouseY += inputSystem.lookY * sens;
                pb.inputCache.mouseY = Mathf.Clamp(pb.inputCache.mouseY, -90, 90);

                pb.inputCache.leanLeft = inputSystem.leanLeft;
                pb.inputCache.leanRight = inputSystem.leanRight;
                pb.inputCache.thirdPerson = inputSystem.changePerspective;
                pb.inputCache.flashlight = inputSystem.flashlight;
                pb.inputCache.laser = inputSystem.laser;

                pb.inputCache.nextWeapon = inputSystem.nextWeapon;
                pb.inputCache.previousWeapon = inputSystem.previousWeapon;

                if (MarsScreen.lockCursor)
                {
                    pb.inputCache.lmb = inputSystem.fireHold;
                }
                else
                {
                    pb.inputCache.lmb = false;
                }

                if (pb.inputCache.weaponSlotUses == null || pb.inputCache.weaponSlotUses.Length != inputSystem.weaponSelectButtons.Length) pb.inputCache.weaponSlotUses = new bool[inputSystem.weaponSelectButtons.Length];

                for (int i = 0; i < inputSystem.weaponSelectButtons.Length; i++)
                {
                    int id = i;
                    pb.inputCache.weaponSlotUses[id] = inputSystem.weaponSelectButtons[id];
                }
            }
            else
            {
                //Get all input
                pb.inputCache.hor = 0f;
                pb.inputCache.ver = 0f;

                pb.inputCache.crouch = false;
                pb.inputCache.sprint = false;
                pb.inputCache.jump = false;
                pb.inputCache.interact = false;

                pb.inputCache.rmb = false;
                pb.inputCache.reload = false;

                pb.inputCache.leanLeft = false;
                pb.inputCache.leanRight = false;
                pb.inputCache.thirdPerson = false;
                pb.inputCache.flashlight = false;
                pb.inputCache.laser = false;

                pb.inputCache.lmb = false;


                if (pb.inputCache.weaponSlotUses == null || pb.inputCache.weaponSlotUses.Length != inputSystem.weaponSelectButtons.Length) pb.inputCache.weaponSlotUses = new bool[inputSystem.weaponSelectButtons.Length];

                for (int i = 0; i < inputSystem.weaponSelectButtons.Length; i++)
                {
                    int id = i;
                    pb.inputCache.weaponSlotUses[id] = false;
                }
            }

            //Set Camera (this is validated on the server)
            pb.inputCache.clientCamPos = pb.playerCameraTransform.position;
            pb.inputCache.clientCamForward = pb.playerCameraTransform.forward;
        }

        public override void ResetAiming(Kit_PlayerBehaviour pb)
        {
            if (pb.inputManagerData != null && pb.inputManagerData.GetType() == typeof(DefaultInputData))
            {
                DefaultInputData did = pb.inputManagerData as DefaultInputData;
                did.resetAim = true;
            }
        }
    }
}