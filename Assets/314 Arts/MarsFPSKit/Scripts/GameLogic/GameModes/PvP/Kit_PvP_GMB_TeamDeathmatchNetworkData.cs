using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace MarsFPSKit
{
    public class Kit_PvP_GMB_TeamDeathmatchNetworkData : Kit_GameModeNetworkDataBase
    {
        /// <summary>
        /// Points scored by each team
        /// </summary>
        public readonly SyncList<int> teamPoints = new SyncList<int>();

        public UnityEvent onPointsChangedServer = new UnityEvent();
        public UnityEvent onPointsChangedClient = new UnityEvent();

        public override void OnStartServer()
        {
            base.OnStartServer();
            teamPoints.Callback += OnCallbackServer;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            teamPoints.Callback += OnCallbackClient;
        }

        private void OnCallbackServer(SyncList<int>.Operation operation, int arg2, int arg3, int arg4)
        {
            onPointsChangedServer.Invoke();
        }

        private void OnCallbackClient(SyncList<int>.Operation operation, int arg2, int arg3, int arg4)
        {
            onPointsChangedClient.Invoke();
        }
    }
}