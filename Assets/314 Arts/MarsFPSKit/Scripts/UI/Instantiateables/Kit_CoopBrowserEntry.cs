using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarsFPSKit.UI
{
    /// <summary>
    /// This class contains references for the coop server browser
    /// </summary>
    public class Kit_CoopBrowserEntry : Kit_Base
    {
        /// <summary>
        /// The name of this room
        /// </summary>
        [SerializeField] private TextMeshProUGUI serverName;
        /// <summary>
        /// The map that is currently played in this room
        /// </summary>
        [SerializeField] private TextMeshProUGUI mapName;
        /// <summary>
        /// How many players are in this room
        /// </summary>
        [SerializeField] private TextMeshProUGUI players;
        /// <summary>
        /// The ping of this room - The cloud
        /// </summary>
        [SerializeField] private TextMeshProUGUI ping;
        /// <summary>
        /// Join Button
        /// </summary>
        [SerializeField] private Button joinButton;

        private Services.GameInfo myRoom;

        /// <summary>
        /// Called from Main Menu to properly set this entry up
        /// </summary>
        public void Setup(Kit_MenuPveGameModeBase menu, Services.GameInfo curRoom, Action onJoin)
        {
            myRoom = curRoom;
            joinButton.onClick.RemoveAllListeners();

            //Reset scale (Otherwise it will be offset)
            transform.localScale = Vector3.one;

            if (myRoom == null) return;

            //Set Info
            serverName.text = myRoom.name;
            //Map
            mapName.text = game.allCoopGameModes[myRoom.gameMode].maps[myRoom.map].mapName;
            //Players
            players.text = myRoom.players + "/" + myRoom.maxPlayers;
            //Ping
            ping.text = myRoom.ping.ToString();
            //Join button setup
            joinButton.onClick.AddListener(delegate { onJoin.Invoke(); });
        }
    }
}