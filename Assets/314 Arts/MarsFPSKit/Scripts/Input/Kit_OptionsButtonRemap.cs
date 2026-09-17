using UnityEngine;
using UnityEngine.InputSystem;

namespace MarsFPSKit
{
    namespace UI
    {
        [CreateAssetMenu(menuName = "MarsFPSKit/Options/Controls/Remap")]
        public class Kit_OptionsButtonRemap : Kit_OptionBase
        {
            /// <summary>
            /// Reference to the input system component in Unity
            /// </summary>
            public InputActionAsset Asset;

            public InputActionReference action;

            public string overrideName;

            /// <summary>
            /// Index of this binding in the action's bindings array
            /// </summary>
            public int BindingIndex;

            public override OptionType GetOptionType()
            {
                return OptionType.Remap;
            }

            public override void OnRemapStart(Kit_OptionRemap remap)
            {
                Load();

                remap.value.text = action.action.bindings[BindingIndex].ToDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice);
            }

            public override void OnRemapOpen(Kit_OptionRemap remap)
            {

            }


            public override void OnRemapApply(Kit_OptionRemap remap)
            {

            }

            public override void OnRemapChange(Kit_OptionRemap remap)
            {
                InitiateRebindOperation(remap, action);
            }


            // <summary>
            /// Remaps the input binding using Unity's own extension.
            /// Code comes from https://docs.unity3d.com/Packages/com.unity.inputsystem@1.1/manual/HowDoI.html#create-a-ui-to-rebind-input-in-my-game
            /// </summary>
            /// <param name="actionToRebind"></param>
            internal void InitiateRebindOperation(Kit_OptionRemap remap, InputAction actionToRebind)
            {
                actionToRebind.Disable();
                var rebindOperation = actionToRebind.PerformInteractiveRebinding(this.BindingIndex)

                    // BUG WORKAROUND: https://forum.unity.com/threads/rebind-bug-arrows-print-screen.807060/
                    .WithControlsExcluding("<Keyboard>/printScreen")

                    // timeout
                    .WithTimeout(2f)

                    // cancel through
                    .WithCancelingThrough("<Keyboard>/escape")

                    // Dispose the operation on completion.
                    .OnComplete(operation =>
                    {
                        operation.Dispose();
                        actionToRebind.Enable();
                        ResetTextAndButtons(remap);
                        remap.options.remapModal.Close();
                        Save();
                    })
                    .OnCancel(operation =>
                    {
                        operation.Dispose();
                        actionToRebind.Enable();
                        ResetTextAndButtons(remap);
                        remap.options.remapModal.Close();
                    })
                    .Start();

                remap.options.remapModal.content.text = action.name;
                remap.options.remapModal.Open();
            }

            public void ResetTextAndButtons(Kit_OptionRemap remap)
            {
                remap.value.text = action.action.bindings[BindingIndex].ToDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice);
            }

            public override void OnRemapReset(Kit_OptionRemap remap)
            {
                action.action.RemoveBindingOverride(BindingIndex);
                ResetTextAndButtons(remap);
                Save();
            }

            void Save()
            {
                PlayerPrefs.SetString("Controls_ " + action.name, action.action.SaveBindingOverridesAsJson());
            }

            void Load()
            {
                string load = PlayerPrefs.GetString("Controls_ " + action.name);

                if (!string.IsNullOrEmpty(load))
                {
                    action.action.LoadBindingOverridesFromJson(load, true);
                }
            }
        }
    }
}