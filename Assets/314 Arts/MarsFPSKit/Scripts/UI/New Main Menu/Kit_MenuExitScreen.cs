using UnityEngine;

namespace MarsFPSKit.UI
{
    public class Kit_MenuExitScreen : Kit_Base
    {
        /// <summary>
        /// Menu Manager reference
        /// </summary>
        public Kit_MenuManager menuManager;
        /// <summary>
        /// Menu id of the exit screen
        /// </summary>
        public int exitScreenId;

        public void Exit()
        {
            //Close game
            Application.Quit();
        }

        public void Abort()
        {
            //Go back to main screen
            menuManager.SwitchMenu(menuManager.mainScreen);
        }
    }
}