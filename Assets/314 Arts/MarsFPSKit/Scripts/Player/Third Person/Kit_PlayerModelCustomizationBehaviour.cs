using UnityEngine;
using UnityEngine.Localization;

namespace MarsFPSKit
{
    /// <summary>
    /// Contains information for attachment slots
    /// </summary>
    [System.Serializable]
    public class CustomizationSlot
    {
        /// <summary>
        /// Name of this slot
        /// </summary>
        public LocalizedString name;

        /// <summary>
        /// The position for the UI dropdown for this slot
        /// </summary>
        public Transform uiPosition;

        /// <summary>
        /// All customizations in this slot
        /// </summary>
        public PlayerModelCustomization[] customizations;
    }

    /// <summary>
    /// A player model customization that can be put in a slot
    /// </summary>
    [System.Serializable]
    public class PlayerModelCustomization
    {
        /// <summary>
        /// Name of this attachment
        /// </summary>
        public LocalizedString name;

        public Kit_PlayerModelCustomizationBehaviour[] customizationBehaviours;
    }


    public abstract class Kit_PlayerModelCustomizationBehaviour : Kit_Base
    {
        /// <summary>
        /// Called when this attachment is selected
        /// </summary>
        public abstract void Selected(Kit_PlayerBehaviour pb, Kit_ThirdPersonPlayerModel pm);

        /// <summary>
        /// Called when this attachment is not selected
        /// </summary>
        public abstract void Unselected(Kit_PlayerBehaviour pb, Kit_ThirdPersonPlayerModel pm);
    }
}