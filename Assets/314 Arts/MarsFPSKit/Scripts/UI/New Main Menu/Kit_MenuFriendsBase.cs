using UnityEngine;

namespace MarsFPSKit.UI
{
    public abstract class Kit_MenuFriendsBase : Kit_Base
    {
        /// <summary>
        /// Menu Manager ID
        /// </summary>
        public Kit_MenuManager menuManager;

        /// <summary>
        /// Id for the friends screen
        /// </summary>
        public int friendsScreenId;

        /// <summary>
        /// Called after the login was successful
        /// </summary>
        public virtual void AfterLogin()
        {

        }

        /// <summary>
        /// Called when the user clicks the button and the menu is opening
        /// </summary>
        public virtual void BeforeOpening()
        {

        }
    }
}