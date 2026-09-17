using Mirror;
using UnityEngine;

namespace MarsFPSKit
{
    /// <summary>
    /// This can be used to make other things damageable
    /// </summary>
    public interface IKitDamageable
    {
        /// <summary>
        /// Called locally for the client that hit
        /// </summary>
        /// <param name="dmg"></param>
        /// <param name="gunID"></param>
        /// <param name="shotPos"></param>
        /// <param name="forward"></param>
        /// <param name="force"></param>
        /// <param name="hitPos"></param>
        /// <param name="shotBot"></param>
        /// <param name="shotId"></param>
        bool LocalDamage(float dmg, int gunID, Vector3 shotPos, Vector3 forward, float force, Vector3 hitPos, bool shotBot, uint shotId);

        /// <summary>
        /// Called for the server after verifying with LagCompensator
        /// </summary>
        /// <param name="dmg"></param>
        /// <param name="gunID"></param>
        /// <param name="shotPos"></param>
        /// <param name="forward"></param>
        /// <param name="force"></param>
        /// <param name="hitPos"></param>
        /// <param name="shotBot"></param>
        /// <param name="shotId"></param>
        void ServerDamage(float dmg, int gunID, Vector3 shotPos, Vector3 forward, float force, Vector3 hitPos, bool shotBot, uint shotId);

        LagCompensator GetLagCompensator();
    }
}