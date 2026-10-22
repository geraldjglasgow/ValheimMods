using EliteEquipment.Core;
using Translations;
using UnityEngine;

namespace EliteEquipment.Weapons
{
    /// <summary>How a weapon's model is worn: one model under <c>attach</c>, a pair skinned to both hands, or claws on both hands.</summary>
    public enum WeaponKind
    {
        Held,
        Dual,
        Fist,
    }

    /// <summary>
    /// One new weapon: the game weapon it is copied from (attacks, animations, stamina, speed, effects, trail), the game
    /// item whose own material its model wears (its UVs lie on that item's atlas), the game weapon its recipe sits beside,
    /// its name word (translations/English.yml: the asset's name, and its description as that + "_desc") and its damage
    /// (per-level damage is the base's, scaled by the same ratio). Made once per process.
    /// </summary>
    public sealed class WeaponSet
    {
        public WeaponKind Kind = WeaponKind.Held;
        public string Prefab;
        public string Asset;
        public string Base;
        public string Beside;
        public string Look;
        public string LookPart;
        public HitData.DamageTypes Damage;
        public RecipePart[] Cost;

        /// <summary>The swing trail's ends in the attach frame; null keeps the base weapon's.</summary>
        public Vector3? TrailBase;
        public Vector3? TrailTip;

        /// <summary>Thrown (spears): the model's place in the copied projectile.</summary>
        public bool Thrown;
        public Vector3 ThrownPosition;
        public Vector3 ThrownRotation;

        /// <summary>The item prefab and its thrown projectile, once made; null when they could not be.</summary>
        public GameObject Item;
        public GameObject Projectile;

        public string Token => Lang.Token(Asset);

        public string DescriptionToken => Lang.Token(Asset + "_desc");
    }
}
