using MarsFPSKit.Weapons;
using UnityEngine;

namespace MarsFPSKit
{
    public class WeaponsFromPlugin
    {
        public WeaponAttachmentBundle[] weaponsInSlot;
    }

    public class WeaponAttachmentBundle
    {
        public Kit_WeaponBase weapon;
        public int[] attachments;
    }

    public abstract class Kit_WeaponInjection : Kit_BaseScriptableObject
    {
        public virtual WeaponsFromPlugin WeaponsToInjectIntoWeaponManager()
        {
            WeaponsFromPlugin weapons = new WeaponsFromPlugin();
            weapons.weaponsInSlot = new WeaponAttachmentBundle[0];
            return weapons;
        }
    }
}