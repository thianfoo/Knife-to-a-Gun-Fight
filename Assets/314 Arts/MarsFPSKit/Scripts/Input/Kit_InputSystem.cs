using UnityEngine;
using UnityEngine.InputSystem;

namespace MarsFPSKit
{
    /// <summary>
    /// Input bridge that exposes simple state (holds/presses/values) using InputActionReference.
    /// Assign the action references in the inspector.
    /// </summary>
    public sealed class Kit_InputSystem : Kit_Base
    {
        public static Kit_InputSystem instance { get; private set; }

        [Header("Action References")]
        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private InputActionReference chatAction;
        [SerializeField] private InputActionReference chatTeamAction;
        [SerializeField] private InputActionReference voiceChatPttAction;
        [SerializeField] private InputActionReference useAction;

        [Header("Weapons")]
        [SerializeField] private InputActionReference[] weaponSelectActions; // size 15
        [SerializeField] private InputActionReference nextWeaponAction;
        [SerializeField] private InputActionReference previousWeaponAction;
        [SerializeField] private InputActionReference scrollWheelAction;     // Vector2 (mouse scroll commonly comes as Vector2)

        [Header("Movement / Look")]
        [SerializeField] private InputActionReference lookAction;            // Vector2
        [SerializeField] private InputActionReference moveAction;            // Vector2
        [SerializeField] private InputActionReference crouchAction;
        [SerializeField] private InputActionReference runAction;
        [SerializeField] private InputActionReference leanLeftAction;
        [SerializeField] private InputActionReference leanRightAction;
        [SerializeField] private InputActionReference jumpAction;

        [Header("Combat / Gear")]
        [SerializeField] private InputActionReference fireAction;
        [SerializeField] private InputActionReference reloadAction;
        [SerializeField] private InputActionReference flashlightAction;
        [SerializeField] private InputActionReference laserAction;
        [SerializeField] private InputActionReference changePerspectiveAction;
        [SerializeField] private InputActionReference meleeAction;
        [SerializeField] private InputActionReference aimAction;

        [Header("UI / Misc")]
        [SerializeField] private InputActionReference scoreboardAction;
        [SerializeField] private InputActionReference voteYesAction;
        [SerializeField] private InputActionReference voteNoAction;
        [SerializeField] private InputActionReference respawnAction;

        // -------------------- Public state (mirrors your original script) --------------------

        public bool pause { get; private set; }
        public bool chat { get; private set; }
        public bool chatTeam { get; private set; }

        public bool voiceChatPtt { get; private set; }
        public bool useHold { get; private set; }

        public bool[] weaponSelectButtons { get; private set; } // size 15
        public bool nextWeapon { get; private set; }
        public bool previousWeapon { get; private set; }

        public float lookX { get; private set; }
        public float lookY { get; private set; }

        [Header("Movement Smoothing")]
        [SerializeField] private float movementInputSmooth = 10f;

        public float movementVerticalRead { get; private set; }
        public float movementVertical { get; private set; }
        public float movementHorizontalRead { get; private set; }
        public float movementHorizontal { get; private set; }

        public bool crouchHold { get; private set; }
        public bool runHold { get; private set; }
        public bool leanLeft { get; private set; }
        public bool leanRight { get; private set; }
        public bool jump { get; private set; }

        public bool fireHold { get; private set; }
        public bool reloadHold { get; private set; }
        public bool flashlight { get; private set; }
        public bool laser { get; private set; }
        public bool changePerspective { get; private set; }
        public bool melee { get; private set; }

        public bool aimHold { get; private set; }
        public bool aimPress { get; private set; }

        public bool scoreboard { get; private set; }

        public bool voteYes { get; private set; }
        public bool voteNo { get; private set; }
        public bool respawn { get; private set; }

        // -------------------- Display string helpers --------------------

        public string GetDisplayString(InputActionReference actionRef)
        {
            var action = actionRef ? actionRef.action : null;
            return action != null ? action.GetBindingDisplayString() : "Invalid Action";
        }

        // -------------------- Unity lifecycle --------------------

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            SetCallbacks(true);

            weaponSelectButtons = new bool[Mathf.Max(0, weaponSelectActions?.Length ?? 0)];
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SetCallbacks(false);
            }
        }

        private void LateUpdate()
        {
            movementVertical = Mathf.Lerp(movementVertical, movementVerticalRead, Time.deltaTime * movementInputSmooth);
            movementHorizontal = Mathf.Lerp(movementHorizontal, movementHorizontalRead, Time.deltaTime * movementInputSmooth);

            // one-frame "press" style flags
            chat = false;
            chatTeam = false;
            pause = false;
            aimPress = false;
            voteYes = false;
            voteNo = false;
            nextWeapon = false;
            previousWeapon = false;
            respawn = false;
        }

        // -------------------- Wiring --------------------

        private void SetCallbacks(bool enable)
        {
            // taps / one-shots
            HookPerformed(pauseAction, OnPause, enable);
            HookPerformed(chatAction, OnChat, enable);
            HookPerformed(chatTeamAction, OnChatTeam, enable);
            HookPerformed(nextWeaponAction, OnNextWeapon, enable);
            HookPerformed(previousWeaponAction, OnPreviousWeapon, enable);
            HookPerformed(voteYesAction, OnVoteYes, enable);
            HookPerformed(voteNoAction, OnVoteNo, enable);
            HookPerformed(respawnAction, OnRespawn, enable);

            // holds / continuous
            HookAny(voiceChatPttAction, OnVoicePtt, enable);
            HookAny(useAction, OnUse, enable);

            HookValueChanged(lookAction, OnLook, enable);
            HookValueChanged(moveAction, OnMove, enable);
            HookAny(scrollWheelAction, OnScrollWheel, enable);

            HookAny(crouchAction, OnCrouch, enable);
            HookAny(runAction, OnRun, enable);
            HookAny(leanLeftAction, OnLeanLeft, enable);
            HookAny(leanRightAction, OnLeanRight, enable);
            HookAny(jumpAction, OnJump, enable);

            HookAny(fireAction, OnFire, enable);
            HookAny(reloadAction, OnReload, enable);
            HookAny(flashlightAction, OnFlashlight, enable);
            HookAny(laserAction, OnLaser, enable);
            HookAny(changePerspectiveAction, OnChangePerspective, enable);
            HookAny(meleeAction, OnMelee, enable);

            HookAny(aimAction, OnAim, enable);
            HookAny(scoreboardAction, OnScoreboard, enable);

            // weapon select buttons
            if (weaponSelectActions != null)
            {
                for (int i = 0; i < weaponSelectActions.Length; i++)
                {
                    int index = i; // capture
                    HookAny(weaponSelectActions[i], ctx => weaponSelectButtons[index] = IsHeld(ctx), enable);
                }
            }

            // make sure actions are enabled/disabled (InputActionReference doesn't auto-enable)
            SetEnabled(pauseAction, enable);
            SetEnabled(chatAction, enable);
            SetEnabled(chatTeamAction, enable);
            SetEnabled(voiceChatPttAction, enable);
            SetEnabled(useAction, enable);

            if (weaponSelectActions != null)
                foreach (var a in weaponSelectActions) SetEnabled(a, enable);

            SetEnabled(nextWeaponAction, enable);
            SetEnabled(previousWeaponAction, enable);
            SetEnabled(scrollWheelAction, enable);

            SetEnabled(lookAction, enable);
            SetEnabled(moveAction, enable);
            SetEnabled(crouchAction, enable);
            SetEnabled(runAction, enable);
            SetEnabled(leanLeftAction, enable);
            SetEnabled(leanRightAction, enable);
            SetEnabled(jumpAction, enable);

            SetEnabled(fireAction, enable);
            SetEnabled(reloadAction, enable);
            SetEnabled(flashlightAction, enable);
            SetEnabled(laserAction, enable);
            SetEnabled(changePerspectiveAction, enable);
            SetEnabled(meleeAction, enable);
            SetEnabled(aimAction, enable);

            SetEnabled(scoreboardAction, enable);
            SetEnabled(voteYesAction, enable);
            SetEnabled(voteNoAction, enable);
            SetEnabled(respawnAction, enable);
        }

        private static void SetEnabled(InputActionReference actionRef, bool enabled)
        {
            if (!actionRef || actionRef.action == null) return;
            if (enabled) actionRef.action.Enable();
            else actionRef.action.Disable();
        }

        private static void HookPerformed(InputActionReference actionRef, System.Action<InputAction.CallbackContext> cb, bool enable)
        {
            if (!actionRef || actionRef.action == null || cb == null) return;
            if (enable) actionRef.action.performed += cb;
            else actionRef.action.performed -= cb;
        }

        private static void HookAny(InputActionReference actionRef, System.Action<InputAction.CallbackContext> cb, bool enable)
        {
            if (!actionRef || actionRef.action == null || cb == null) return;

            if (enable)
            {
                actionRef.action.started += cb;
                actionRef.action.performed += cb;
                actionRef.action.canceled += cb;
            }
            else
            {
                actionRef.action.started -= cb;
                actionRef.action.performed -= cb;
                actionRef.action.canceled -= cb;
            }
        }

        private static void HookValueChanged(InputActionReference actionRef, System.Action<InputAction.CallbackContext> cb, bool enable)
        {
            // For value actions we usually just need performed + canceled (started is optional).
            if (!actionRef || actionRef.action == null || cb == null) return;

            if (enable)
            {
                actionRef.action.performed += cb;
                actionRef.action.canceled += cb;
            }
            else
            {
                actionRef.action.performed -= cb;
                actionRef.action.canceled -= cb;
            }
        }

        private static bool IsHeld(InputAction.CallbackContext context)
            => context.phase != InputActionPhase.Canceled;

        // -------------------- Callbacks --------------------

        private void OnPause(InputAction.CallbackContext _)
            => pause = true;

        private void OnChat(InputAction.CallbackContext _)
            => chat = true;

        private void OnChatTeam(InputAction.CallbackContext _)
            => chatTeam = true;

        private void OnVoicePtt(InputAction.CallbackContext ctx)
            => voiceChatPtt = IsHeld(ctx);

        private void OnUse(InputAction.CallbackContext ctx)
            => useHold = IsHeld(ctx);

        private void OnNextWeapon(InputAction.CallbackContext _)
            => nextWeapon = true;

        private void OnPreviousWeapon(InputAction.CallbackContext _)
            => previousWeapon = true;

        private void OnScrollWheel(InputAction.CallbackContext ctx)
        {
            var scroll = ctx.ReadValue<Vector2>();

            if (scroll.y < -0.2f) nextWeapon = true;
            else if (scroll.y > 0.2f) previousWeapon = true;
        }

        private void OnLook(InputAction.CallbackContext ctx)
        {
            var look = ctx.ReadValue<Vector2>() / 10f;
            lookX = look.x;
            lookY = look.y;
        }

        private void OnMove(InputAction.CallbackContext ctx)
        {
            var move = ctx.ReadValue<Vector2>();
            movementHorizontalRead = move.x;
            movementVerticalRead = move.y;
        }

        private void OnCrouch(InputAction.CallbackContext ctx)
            => crouchHold = IsHeld(ctx);

        private void OnRun(InputAction.CallbackContext ctx)
            => runHold = IsHeld(ctx);

        private void OnLeanLeft(InputAction.CallbackContext ctx)
            => leanLeft = IsHeld(ctx);

        private void OnLeanRight(InputAction.CallbackContext ctx)
            => leanRight = IsHeld(ctx);

        private void OnJump(InputAction.CallbackContext ctx)
            => jump = IsHeld(ctx);

        private void OnFire(InputAction.CallbackContext ctx)
            => fireHold = IsHeld(ctx);

        private void OnReload(InputAction.CallbackContext ctx)
            => reloadHold = IsHeld(ctx);

        private void OnFlashlight(InputAction.CallbackContext ctx)
            => flashlight = IsHeld(ctx);

        private void OnLaser(InputAction.CallbackContext ctx)
            => laser = IsHeld(ctx);

        private void OnChangePerspective(InputAction.CallbackContext ctx)
            => changePerspective = IsHeld(ctx);

        private void OnMelee(InputAction.CallbackContext ctx)
            => melee = IsHeld(ctx);

        private void OnAim(InputAction.CallbackContext ctx)
        {
            aimHold = IsHeld(ctx);
            if (ctx.phase == InputActionPhase.Performed)
                aimPress = true;
        }

        private void OnScoreboard(InputAction.CallbackContext ctx)
            => scoreboard = IsHeld(ctx);

        private void OnVoteYes(InputAction.CallbackContext _)
            => voteYes = true;

        private void OnVoteNo(InputAction.CallbackContext _)
            => voteNo = true;

        private void OnRespawn(InputAction.CallbackContext _)
            => respawn = true;
    }
}
