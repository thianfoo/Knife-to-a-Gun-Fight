using System.Linq;
using MarsFPSKit.UI;
using Mirror;
using UnityEngine;

namespace MarsFPSKit
{
    public class Kit_PvE_SandboxMenuNetworkData : Kit_BaseNetworked
    {
        [SyncVar(hook = nameof(OnCoopMapChanged))]
        /// <summary>
        /// Currently selected map
        /// </summary>
        public int coopCurMap;

        public Kit_PvE_SandboxMenu myMenu;

        public override void OnStartServer()
        {
            if (!myMenu)
            {
                myMenu = FindObjectsByType<Kit_PvE_SandboxMenu>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(x => x.myCurrentState == 1).FirstOrDefault();
                if (myMenu)
                {
                    myMenu.coopNetworkData = this;
                    myMenu.OnJoinedRoom();
                }
            }
        }

        public override void OnStartClient()
        {
            if (!myMenu)
            {
                myMenu = FindObjectsByType<Kit_PvE_SandboxMenu>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(x => x.myCurrentState == 1).FirstOrDefault();
                if (myMenu)
                {
                    myMenu.coopNetworkData = this;
                    myMenu.OnStartClient();
                }
            }
        }

        public override void OnStopClient()
        {
            if (myMenu)
            {
                myMenu.OnStopClient();
            }
        }

        public void OnCoopMapChanged(int was, int isNow)
        {
            if (myMenu)
            {
                myMenu.OnCoopMapChanged(was, isNow);
            }
        }
    }
}