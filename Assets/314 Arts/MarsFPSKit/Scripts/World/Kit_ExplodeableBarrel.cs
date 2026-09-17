using Mirror;
using UnityEngine;

namespace MarsFPSKit
{
    public class Kit_ExplodeableBarrel : Kit_BaseNetworked, IKitDamageable
    {
        /// <summary>
        /// Explosion prefab
        /// </summary>
        public GameObject explosion;
        /// <summary>
        /// Hitpoints at start.
        /// </summary>
        public float startHitPoints = 100f;
        [SyncVar]
        /// <summary>
        /// Current hitpoints
        /// </summary>
        private float hitPoints = 100f;
        /// <summary>
        /// If set to > 0, the hitpoints will decrease by this factor after they have been damaged once
        /// </summary>
        public float decreaseHitPointsAfterDamaged = 10f;
        /// <summary>
        /// This particle system will be played when it was damaged
        /// </summary>
        public ParticleSystem playWhenDamaged;

        /// <summary>
        /// Was the player who destroyed this barrel a bot?
        /// </summary>
        private bool destroyedByBot;
        /// <summary>
        /// The id of the player that destroyed the barrel
        /// </summary>
        private uint destroyedById = 0;

        private bool wasDestroyed;

        public override void OnStartServer()
        {
            hitPoints = startHitPoints;
        }

        void Update()
        {
            if (hitPoints < startHitPoints)
            {
                if (isServer)
                {
                    if (decreaseHitPointsAfterDamaged > 0)
                    {
                        hitPoints -= Time.deltaTime * decreaseHitPointsAfterDamaged;

                        if (hitPoints <= 0)
                        {
                            if (!wasDestroyed)
                            {
                                NetworkServer.Destroy(gameObject);
                                wasDestroyed = true;
                            }
                        }
                    }
                }

                if (playWhenDamaged)
                {
                    if (!playWhenDamaged.isPlaying)
                    {
                        playWhenDamaged.Play(true);
                    }
                }
            }
        }
        //If they are scene objects, they will only be disabled on clients
        //Either way ondisabled should be called

        void OnDisable()
        {
            if (explosion && !sceneSyncer.isLoading)
            {
                GameObject go = Instantiate(explosion, transform.position, transform.rotation);

                if (go.GetComponent<Kit_Explosion>())
                {
                    go.GetComponent<Kit_Explosion>().Explode(isServer, destroyedByBot, destroyedById, "Barrel");
                }
            }
        }

        public GameObject GetRootGameObject()
        {
            return null;
        }

        bool IKitDamageable.LocalDamage(float dmg, int gunID, Vector3 shotPos, Vector3 forward, float force, Vector3 hitPos, bool shotBot, uint shotId)
        {
            return hitPoints > 0;
        }

        void IKitDamageable.ServerDamage(float dmg, int gunID, Vector3 shotPos, Vector3 forward, float force, Vector3 hitPos, bool shotBot, uint shotId)
        {
            if (hitPoints > 0)
            {
                hitPoints -= dmg;
                destroyedByBot = shotBot;
                destroyedById = shotId;

                if (hitPoints <= 0)
                {
                    NetworkServer.Destroy(gameObject);
                }
            }
        }

        LagCompensator IKitDamageable.GetLagCompensator()
        {
            return GetComponent<LagCompensator>();
        }
    }
}