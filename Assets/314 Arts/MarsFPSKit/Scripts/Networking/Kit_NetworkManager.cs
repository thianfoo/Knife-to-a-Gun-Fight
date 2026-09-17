using UnityEngine;
using Mirror;
using UnityEngine.Events;
using MarsFPSKit.Services;
using Mirror.Discovery;
using Unity.Services.Lobbies.Models;
using MarsFPSKit.Networking;
using System.Linq;
using System.Collections.Generic;
using kcp2k;
using MarsFPSKit.UI;
#if INTEGRATION_DEDICATEDSERVERKIT
using DedicatedServerKit;
#endif

namespace MarsFPSKit
{
#if INTEGRATION_DEDICATEDSERVERKIT
#if UNITY_SERVER
    public class Kit_NetworkManager : GameServer
    {
#else

    public class Kit_NetworkManager : NetworkManager 
    { 
#endif
#else
    public class Kit_NetworkManager : NetworkManager
    {
#endif
        /// <summary>
        /// Instance of the Kit's network manager
        /// </summary>
        public static Kit_NetworkManager instance
        {
            get
            {
                return singleton as Kit_NetworkManager;
            }
        }

        /// <summary>
        /// Are we running in offline mode currently?
        /// </summary>
        [System.NonSerialized]
        public bool isOfflineMode;

        [Header("MMFPSE")]
        [Tooltip("Reference to the game configuration file")]
        /// <summary>
        /// Reference to the game configuration file
        /// </summary>
        public Kit_GameInformation game;

        /// <summary>
        /// Prefab of the information holding game object
        /// </summary>
        [Tooltip("Prefab of the information holding game object")]
        [Header("Prefabs")]
        public GameObject networkGameInformationPrefab;
        /// <summary>
        /// This game object contains information about all the players
        /// </summary>
        [Tooltip("This game object contains information about all the players")]
        public GameObject networkPlayerManagerPrefab;
        [Tooltip("Mirror requires the player to have a player object for sync purposes. This is the placeholder until the actual player is spawned.")]
        /// <summary>
        /// Mirror requires the player to have a player object for sync purposes. This is the placeholder until the actual player is spawned.
        /// </summary>
        public GameObject networkPlayerPlaceholder;

        /// <summary>
        /// This is invoked after the server was succesfully started
        /// </summary>
        public UnityEvent onHostStartedEvent = new UnityEvent();

        /// <summary>
        /// This is invoked after we connected to a server
        /// </summary>
        public UnityEvent onConnectedToServer = new UnityEvent();

        #region Runtime Data
        public float sendRateDelta;
        #endregion

        #region Unity Calls
        public override void Awake()
        {
            //Initialize transport. This has to be assigned!
            game.transport.Initialize(this);

#if INTEGRATION_DEDICATEDSERVERKIT
            if (game.transport is not Kit_TransportServiceDSK)
            {
                DebugLogError("When using Dedicated Server Kit integration, the transport has to be of type Kit_TransportServiceDSK!");
            }
#endif

            //Give plugins their call Ids
            for (int i = 0; i < game.plugins.Length; i++)
            {
                game.plugins[i].startEventCode = (byte)(16 + 16 * i);
            }

            base.Awake();
        }

#if INTEGRATION_DEDICATEDSERVERKIT
#if !UNITY_SERVER
        /// <summary>
        /// The client configuration to use by the HTTP client. You set this from the Inspector.
        /// </summary>
        public ClientConfig Config;
#endif
#endif

        private void OnEnable()
        {
            //Automatically add it to the list
            spawnPrefabs.Add(networkGameInformationPrefab);
            spawnPrefabs.Add(networkPlayerManagerPrefab);
            spawnPrefabs.Add(networkPlayerPlaceholder);

            sendRateDelta = 1f / sendRate;
        }
        #endregion

        #region Custom Calls
        /// <summary>
        /// Host (Server + Client) a game using kit methods.
        /// </summary>
        /// <param name="after"></param>
        public void HostGame(int playersLimit = 4, bool offline = false)
        {
            if (NetworkClient.active)
            {
                StopClient();
            }

            if (NetworkServer.active)
            {
                StopServer();
            }

            if (offline)
            {
                //Destroy transports
                Transport[] transports = GetComponents<Transport>();

                for (int i = 0; i < transports.Length; i++)
                {
                    if (transports[i] is not KcpTransport)
                    {
                        Destroy(transports[i]);
                    }
                }

                if (!gameObject.GetComponent<KcpTransport>())
                {
                    //Switch to KcpTransport
                    transport = gameObject.AddComponent<KcpTransport>();
                    Transport.active = transport;
                }

                //Only one (ourselves)
                maxConnections = 1;

                isOfflineMode = true;

#if UNITY_SERVER
                StartServer();
#else
                StartHost();
#endif

            }
            else
            {
                isOfflineMode = false;

                maxConnections = playersLimit;

#if UNITY_SERVER
                //Relay to service
                game.transport.StartServer(this);
#else
                //Relay to service
                game.transport.StartHost(this);
#endif

            }
        }

        public void JoinGame(string connectionString)
        {
            if (NetworkClient.active)
            {
                StopClient();
            }

            isOfflineMode = false;

            //Relay to service
            game.transport.ConnectWithString(this, connectionString);
        }

        public void JoinGame(GameInfo gameInfo)
        {
            if (NetworkClient.active)
            {
                StopClient();
            }

            isOfflineMode = false;

            //Relay to service
            game.transport.ConnectWithString(this, gameInfo.connection);
        }

        public void JoinGame(GameInfo gameInfo, string password)
        {
            if (NetworkClient.active)
            {
                StopClient();
            }

            isOfflineMode = false;

            //Relay to service
            game.transport.ConnectWithStringAndPassword(this, gameInfo.connection, password);
        }
        #endregion

        #region Mirror Calls
        public override void OnStartServer()
        {
            RegisterCustomPrefabs();

            //Create player manager
            GameObject pm = Instantiate(networkPlayerManagerPrefab);
            NetworkServer.Spawn(pm);

#if UNITY_SERVER
            //As dedicated server there is no need to wait for other things
            if (onHostStartedEvent != null)
            {
                onHostStartedEvent.Invoke();
            }
#endif
        }

        public override void OnStartHost()
        {

        }

        public override void OnStartClient()
        {
            //Register them only now when we are not a host (we already registered them in OnStartServer)
            if (!NetworkServer.active)
            {
                RegisterCustomPrefabs();
            }
        }

        public override void OnStopClient()
        {

        }

        public override void OnStopHost()
        {

        }

        public override void OnStopServer()
        {

        }

        public override void OnServerChangeScene(string newSceneName)
        {
            //If we are host (listen server), display loading screen aswell.
            if (mode == NetworkManagerMode.Host)
            {
                sceneSyncer.StartCoroutine(sceneSyncer.DisplayAsyncSceneLoading(newSceneName));
            }
        }

        public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
        {
            //Display loading screen
            sceneSyncer.StartCoroutine(sceneSyncer.DisplayAsyncSceneLoading(newSceneName));
        }

        public override void OnClientSceneChanged()
        {
            NetworkClient.Ready();
            NetworkClient.AddPlayer();
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {

        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Kit_Player leftPlayer = networkPlayerManager.players.Where(x => x.serverToClientConnection == conn).FirstOrDefault();
            if (leftPlayer != null)
            {
                networkPlayerManager.players.Remove(leftPlayer);
            }

            base.OnServerDisconnect(conn);
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Kit_Player playerData = networkPlayerManager.GetPlayerByConnection(conn);

            GameObject playerGameObject = Instantiate(networkPlayerPlaceholder);
            Kit_PlayerPlaceholder playerPlaceholderData = playerGameObject.GetComponent<Kit_PlayerPlaceholder>();
            //Just for server.
            playerGameObject.name = "Player Placeholder (" + conn.connectionId + ")";
            //Tell him his assigned id
            playerPlaceholderData.id = playerData.id;
            NetworkServer.AddPlayerForConnection(conn, playerGameObject);
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
        }

        public override void ServerChangeScene(string newSceneName)
        {
            Kit_PlayerBehaviour[] players = FindObjectsByType<Kit_PlayerBehaviour>(FindObjectsSortMode.None);

            for (int i = 0; i < players.Length; i++)
            {
                NetworkServer.Destroy(players[i].gameObject);
            }

            Kit_PlayerPlaceholder[] playerPlaceHolders = FindObjectsByType<Kit_PlayerPlaceholder>(FindObjectsSortMode.None);

            for (int i = 0; i < playerPlaceHolders.Length; i++)
            {
                NetworkServer.Destroy(playerPlaceHolders[i].gameObject);
            }

            base.ServerChangeScene(newSceneName);
        }
        #endregion

        #region Custom Calls
        /// <summary>
        /// Called when custom prefabs should be registered.
        /// I don't really like dragging everything in manually in the list, it leaves too much room for error.
        /// </summary>
        private void RegisterCustomPrefabs()
        {
            List<GameObject> registeredAlready = new List<GameObject>();

            for (int i = 0; i < game.allWeapons.Length; i++)
            {
                if (game.allWeapons[i].runtimeDataPrefab)
                {
                    if (!registeredAlready.Contains(game.allWeapons[i].runtimeDataPrefab))
                    {
                        registeredAlready.Add(game.allWeapons[i].runtimeDataPrefab);
                        NetworkClient.RegisterPrefab(game.allWeapons[i].runtimeDataPrefab);
                    }
                }
                else
                {
                    Debug.LogWarning("Weapon without runtime data prefab found", game.allWeapons[i].runtimeDataPrefab);
                }

                game.allWeapons[i].RegisterNetworkPrefabs();
            }

            Kit_PlayerBehaviour dataHolderPb = playerPrefab.GetComponent<Kit_PlayerBehaviour>();

            //Movement
            NetworkClient.RegisterPrefab(dataHolderPb.movement.networkData);

            //Mouse Look
            NetworkClient.RegisterPrefab(dataHolderPb.looking.networkData);

            //Weapon Manager
            NetworkClient.RegisterPrefab(dataHolderPb.weaponManager.networkData);

            //Vitals
            NetworkClient.RegisterPrefab(dataHolderPb.vitalsManager.networkData);

            Kit_IngameMain dataHolderMain = game.ingameMainPrefab.GetComponent<Kit_IngameMain>();

            //Victory screen
            NetworkClient.RegisterPrefab(dataHolderMain.victoryScreen);

            //Map Voting
            NetworkClient.RegisterPrefab(dataHolderMain.mapVoting);

            //PVP Game Modes
            for (int i = 0; i < game.allPvpGameModes.Length; i++)
            {
                if (game.allPvpGameModes[i].networkData)
                {
                    if (!NetworkClient.prefabs.ContainsKey(game.allPvpGameModes[i].networkData.GetComponent<NetworkIdentity>().assetId))
                    {
                        if (!NetworkClient.prefabs.ContainsValue(game.allPvpGameModes[i].networkData))
                        {
                            NetworkClient.RegisterPrefab(game.allPvpGameModes[i].networkData);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Game mode does not have a network data assigned", game.allPvpGameModes[i]);
                }

                game.allPvpGameModes[i].RegisterNetworkPrefabs();
            }

            //PVE Game Modes
            for (int i = 0; i < game.allCoopGameModes.Length; i++)
            {
                if (game.allCoopGameModes[i].menuPrefab && game.allCoopGameModes[i].menuPrefab.GetComponent<NetworkIdentity>())
                {
                    if (!NetworkClient.prefabs.ContainsKey(game.allCoopGameModes[i].menuPrefab.GetComponent<NetworkIdentity>().assetId))
                    {
                        NetworkClient.RegisterPrefab(game.allCoopGameModes[i].menuPrefab);
                    }
                }

                if (game.allCoopGameModes[i].networkData)
                {
                    if (!NetworkClient.prefabs.ContainsKey(game.allCoopGameModes[i].networkData.GetComponent<NetworkIdentity>().assetId))
                    {
                        NetworkClient.RegisterPrefab(game.allCoopGameModes[i].networkData);
                    }
                }
                else
                {
                    Debug.LogWarning("Game mode does not have a network data assigned", game.allCoopGameModes[i]);
                }

                if (game.allCoopGameModes[i].menuPrefab)
                {
                    Kit_MenuPveGameModeBase menu = game.allCoopGameModes[i].menuPrefab.GetComponent<Kit_MenuPveGameModeBase>();

                    menu.RegisterCustomPrefabs();
                }

                game.allCoopGameModes[i].RegisterNetworkPrefabs();
            }

            for (int i = 0; i < game.plugins.Length; i++)
            {
                game.plugins[i].RegisterCustomPrefabs();
            }
        }
        #endregion

        #region Convenience Accesses
        protected Kit_IngameMain main
        {
            get
            {
                return Kit_IngameMain.instance;
            }
        }

        protected Kit_NetworkPlayerManager networkPlayerManager
        {
            get
            {
                return Kit_NetworkPlayerManager.instance;
            }
        }

        protected Kit_NetworkGameInformation networkGameInformation
        {
            get
            {
                return Kit_NetworkGameInformation.instance;
            }
        }

        protected Kit_SceneSyncer sceneSyncer
        {
            get
            {
                return Kit_SceneSyncer.instance;
            }
        }

        protected Kit_NetworkManager networkManager
        {
            get
            {
                return Kit_NetworkManager.instance;
            }
        }

        protected Kit_InputSystem inputSystem
        {
            get
            {
                return Kit_InputSystem.instance;
            }
        }
        #endregion
    }
}