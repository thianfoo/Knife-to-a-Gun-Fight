using UnityEngine;

namespace MarsFPSKit.Weapons
{
    /// <summary>
    /// This script should be on weapon shells. It plays sounds upon collision and destorys the game object after some time
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Kit_WeaponShell : Kit_Base
    {
        /// <summary>
        /// The lifetime of this shell in seconds
        /// </summary>
        public float lifeTime = 15f;

        /// <summary>
        /// The relative magnitude threshold before we play sounds
        /// </summary>
        public float impactSoundThreshold = 2f;
        /// <summary>
        /// Shell sounds to play upon collision
        /// </summary>
        public AudioClip[] impactSounds;


        private void OnEnable()
        {
            //Automatically destroy this gameobject after lifetime is over
            Invoke("DestroyPooled", lifeTime);
        }

        void OnCollisionEnter(Collision collision)
        {
            //Check if we have sounds assigned
            if (impactSounds.Length > 0)
            {
                //Check magnitude
                if (collision.relativeVelocity.magnitude > impactSoundThreshold)
                {
                    //Play random sound
                    GetComponent<AudioSource>().clip = impactSounds[Random.Range(0, impactSounds.Length)];
                    GetComponent<AudioSource>().Play();
                }
            }
        }

        void DestroyPooled()
        {
            main.objectPooling.DestroyInstantiateable(gameObject);
        }
    }
}
