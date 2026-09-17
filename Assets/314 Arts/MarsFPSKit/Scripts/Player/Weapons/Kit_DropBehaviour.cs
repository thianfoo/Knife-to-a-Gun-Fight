using MarsFPSKit.Networking;
using Mirror;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace MarsFPSKit
{
    namespace Weapons
    {
        public class Kit_DropBehaviour : Kit_BaseNetworked
        {
            public Rigidbody body;

            public GameObject rendererRoot;
            /// <summary>
            /// How long is this weapon drop going to be live?
            /// </summary>
            public float lifeTime = 30f;

            [SyncVar]
            public int weaponID;
            [SyncVar]
            public int bulletsLeft;
            [SyncVar]
            public int bulletsLeftToReload;
            public readonly SyncList<int> attachments = new SyncList<int>();
            [SyncVar]
            public bool isSceneOwned;

            public override void OnStartServer()
            {
                //Instantiate renderer
                GameObject pr = Instantiate(main.gameInformation.allWeapons[weaponID].dropPrefab, rendererRoot.transform);
                //Set scale
                pr.transform.localScale = Vector3.one;
                if (main.gameInformation.allWeapons[weaponID] is Kit_ModernWeaponScript)
                {
                    //Get Renderer
                    Kit_DropRenderer render = pr.GetComponent<Kit_DropRenderer>();
                    //Setup Attachments
                    render.SetAttachments(main.gameInformation.allWeapons[weaponID] as Kit_ModernWeaponScript, attachments.ToArray());
                }

                if (!isSceneOwned)
                {
                    if (lifeTime > 0)
                    {
                        StartCoroutine(DestroyTimed());
                    }
                }
            }

            public override void OnStartClient()
            {
                if (!isServer)
                {
                    //Instantiate renderer
                    GameObject pr = Instantiate(main.gameInformation.allWeapons[weaponID].dropPrefab, rendererRoot.transform);
                    //Set scale
                    pr.transform.localScale = Vector3.one;
                    if (main.gameInformation.allWeapons[weaponID] is Kit_ModernWeaponScript)
                    {
                        //Get Renderer
                        Kit_DropRenderer render = pr.GetComponent<Kit_DropRenderer>();
                        //Setup Attachments
                        render.SetAttachments(main.gameInformation.allWeapons[weaponID] as Kit_ModernWeaponScript, attachments.ToArray());
                    }
                }
            }

            IEnumerator DestroyTimed()
            {
                yield return new WaitForSeconds(lifeTime);
                NetworkServer.Destroy(gameObject);
            }

            [Command(requiresAuthority = false)]
            public void CmdTryPickUp(NetworkConnectionToClient who = null)
            {
                if (NetworkServer.active)
                {
                    Kit_PlayerBehaviour player = networkPlayerManager.GetPlayerObjectByConnection(who);

                    if (player)
                    {
                        if (Vector3.Distance(transform.position, player.transform.position) < 3)
                        {
                            int slot = 0;
                            int currentWeapon = player.weaponManager.GetCurrentlySelectedWeapon(player);

                            if (main.gameInformation.allWeapons[weaponID].canFitIntoSlots.Contains(currentWeapon))
                            {
                                slot = currentWeapon;
                            }
                            else
                            {
                                slot = main.gameInformation.allWeapons[weaponID].canFitIntoSlots[0];
                            }

                            //Check if we can drop
                            if (!isSceneOwned || isSceneOwned && main.gameInformation.enableDropWeaponOnSceneSpawnedWeapons)
                            {
                                //First drop our weapon
                                player.weaponManager.DropWeapon(player, slot, transform);
                            }

                            player.ServerReplaceWeapon(slot, weaponID, bulletsLeft, bulletsLeftToReload, attachments.ToArray());
                            NetworkServer.Destroy(gameObject);
                        }
                    }
                }
            }
        }
    }
}