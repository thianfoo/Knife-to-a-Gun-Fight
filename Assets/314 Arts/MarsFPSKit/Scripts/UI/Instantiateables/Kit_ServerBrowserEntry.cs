using System;
using MarsFPSKit.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarsFPSKit.UI
{
    /// <summary>
    /// This class contains references for the server browser and acts as a sender  
    /// </summary>
    public class Kit_ServerBrowserEntry : Kit_Base
    {
        [SerializeField] private TextMeshProUGUI serverName; //The name of this room
        [SerializeField] private TextMeshProUGUI dedicated; //Displays if its a dedicated server
        [SerializeField] private TextMeshProUGUI mapName; //The map that is currently played in this room
        [SerializeField] private TextMeshProUGUI gameModeName; //The game mode that is currently played in this room
        [SerializeField] private TextMeshProUGUI players; //How many players are in this room
        [SerializeField] private TextMeshProUGUI ping; //The ping of this room - The cloud
        [SerializeField] private TextMeshProUGUI password; //If this room is password protected
        private Kit_MenuServerBrowser msb;
        private GameInfo myRoom;

        /// <summary>
        /// Called from Main Menu to properly set this entry up
        /// </summary>
        public void Setup(Kit_MenuServerBrowser curMsb, GameInfo curRoom)
        {
            msb = curMsb;
            myRoom = curRoom;

            //Reset scale (Otherwise it will be offset)
            transform.localScale = Vector3.one;

            if (myRoom == null) return;

            //Set Info
            serverName.text = myRoom.name;
            int gameMode = myRoom.gameMode;
            //Game Mode
            gameModeName.text = game.allPvpGameModes[gameMode].gameModeName.GetLocalizedString();
            //Map
            mapName.text = game.allPvpGameModes[gameMode].traditionalMaps[myRoom.map].mapName;
            bool bots = myRoom.bots;
            if (bots)
            {
                //Players
                players.text = myRoom.players + "/" + myRoom.maxPlayers + " (bots)";
            }
            else
            {
                //Players
                players.text = myRoom.players + "/" + myRoom.maxPlayers;
            }
            //Ping
            ping.text = myRoom.ping.ToString();
            //Password
            if (myRoom.password) password.text = "Yes";
            else password.text = "No";

            if (myRoom.dedicated)
            {
                dedicated.text = "Yes";
            }
            else
            {
                dedicated.text = "No";
            }
        }

        //Called from the button that is on this prefab, to join this room (attempt)
        public void OnClick()
        {
            //Check if this button is ready
            if (msb)
            {
                if (myRoom != null)
                {
                    //Attempt to join
                    msb.JoinRoom(myRoom);
                }
            }
        }
    }
}