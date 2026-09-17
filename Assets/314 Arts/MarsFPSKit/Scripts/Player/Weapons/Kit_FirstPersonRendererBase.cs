using UnityEngine;

namespace MarsFPSKit.Weapons
{
    public class Kit_FirstPersonRendererBase : Kit_Base
    {
        /// <summary>
        /// The weapon animator
        /// </summary>
        public Animator anim;

        /// <summary>
        /// The weapon renderers that are not of attachments
        /// </summary>
        [Tooltip("Asssign all weapon renderers here that are not of attachments")]
        public Renderer[] allWeaponRenderers;
        /// <summary>
        /// These rendererers will be disabled in the customization menu. E.g. arms
        /// </summary>
        public Renderer[] hideInCustomiazionMenu;
    }
}
