using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarsFPSKit.UI
{
    public class Kit_MenuSingleplayer : Kit_Base
    {
        /// <summary>
        /// Access to menu manager!
        /// </summary>
        public Kit_MenuManager menuManager;
        /// <summary>
        /// Id of this screen in the manager
        /// </summary>
        public int singleplayerScreenId;
        /// <summary>
        /// Go for the layout where we select the game modes
        /// </summary>
        public RectTransform layoutGo;
        /// <summary>
        /// Prefabs for the layout where we select the game modes
        /// </summary>
        public GameObject layoutPrefab;

        private void Start()
        {
            for (int i = 0; i < game.allSingleplayerGameModes.Length; i++)
            {
                int id = i;
                Kit_PvE_GameModeBase gameMode = game.allSingleplayerGameModes[id];

                if (gameMode.menuPrefab)
                {
                    GameObject menu = Instantiate(gameMode.menuPrefab);

                    Kit_MenuPveGameModeBase pveMenu = menu.GetComponent<Kit_MenuPveGameModeBase>();

                    if (pveMenu)
                    {
                        //Setup
                        pveMenu.SetupMenu(menuManager, 0, id);

                        //Create button
                        GameObject go = Instantiate(layoutPrefab, layoutGo, false);
                        //Set pos
                        go.transform.SetSiblingIndex(i);
                        //Get button
                        Button btn = go.GetComponentInChildren<Button>();
                        btn.onClick.AddListener(delegate { pveMenu.OpenMenu(); });
                        //Name
                        TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
                        txt.text = gameMode.gameModeName.GetLocalizedString();
                    }
                    else
                    {
                        DebugLog("[Singleplayer] Game Mode " + gameMode.name + " has no menu script on its prefab", menu);
                    }
                }
                else
                {
                    DebugLog("[Singleplayer] Game Mode " + gameMode.name + " has no menu", gameMode);
                }
            }
        }
    }
}