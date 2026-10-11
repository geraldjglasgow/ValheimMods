using System.Collections.Generic;
using System.Linq;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// A definition's <c>damage:</c> and <c>projectile:</c>, which reach every attack the creature has: each attack item
    /// it carries (its weapons and its creature attacks, in every list and set) is swapped for the creature's own copy
    /// and changed there (<see cref="OwnItems"/>), and so is every attack the same definition adds
    /// (<see cref="NewAttacks"/>, which then puts the attack's own values over these). A human's own fists
    /// (<c>HumanFists</c>, already its own) take the damage too; another creature's unarmed weapon is the game's own and is
    /// left alone. The projectile reaches every projectile attack and every piece of ammunition carrying one; a bow or
    /// crossbow carried without its ammunition shoots the arrows or bolts it is given as it spawns, which the swap cannot
    /// reach, so that is a warning.
    /// </summary>
    internal sealed class CreatureWide
    {
        private readonly CreatureBuild build;
        private readonly DamageBlock? damage;
        private readonly GameObject? projectile;

        private CreatureWide(CreatureBuild build, DamageBlock? damage, GameObject? projectile)
        {
            this.build = build;
            this.damage = damage;
            this.projectile = projectile;
        }

        /// <summary>The definition's creature-wide values, or null when it sets neither (or names an unknown projectile,
        /// which fails the creature).</summary>
        public static CreatureWide? Read(CreatureBuild build)
        {
            CreatureDefinition definition = build.Definition;
            GameObject? projectile = definition.Projectile != null ? ProjectileRules.Find(build, definition.Projectile, "projectile") : null;
            if (build.Report.Failed || (definition.Damage == null && projectile == null))
            {
                return null;
            }
            return new CreatureWide(build, definition.Damage, projectile);
        }

        /// <summary>Changes every attack item the creature carries, each swapped for its own copy first.</summary>
        public void ApplyToCarried(Humanoid humanoid)
        {
            foreach (GameObject item in CarriedItems.All(humanoid).Where(Touches).ToList())
            {
                ApplyTo(OwnItems.Own(build, humanoid, item));
            }
            Fists(humanoid);
            if (projectile != null)
            {
                WarnAmmoless(humanoid);
            }
        }

        /// <summary>Changes one of the creature's own items (a copy), as for everything it carries.</summary>
        public void ApplyTo(GameObject own)
        {
            ItemDrop.ItemData.SharedData shared = OwnItems.Shared(own);
            if (damage != null && CarriedItems.IsAttack(own))
            {
                DamageRules.Apply(build, shared, damage, "damage", OwnItems.SourceOf(build, own));
            }
            if (projectile != null && ProjectileRules.Fires(shared))
            {
                ProjectileRules.Swap(shared, projectile);
            }
        }

        private bool Touches(GameObject item)
        {
            ItemDrop? drop = item.GetComponent<ItemDrop>();
            if (drop == null || drop.m_itemData.m_shared == null)
            {
                return false;
            }
            return (damage != null && CarriedItems.IsAttack(item)) || (projectile != null && ProjectileRules.Fires(drop.m_itemData.m_shared));
        }

        /// <summary>A human's fists are its own copy (an inactive child of its prefab): only their damage is changed, a
        /// number field of their own shared data, never their attack, which they still share with the player's.</summary>
        private void Fists(Humanoid humanoid)
        {
            ItemDrop? fists = humanoid.m_unarmedWeapon;
            if (damage != null && fists != null && fists.transform.IsChildOf(build.Shell.transform))
            {
                DamageRules.Apply(build, fists.m_itemData.m_shared, damage, "damage", "unarmed");
            }
        }

        private void WarnAmmoless(Humanoid humanoid)
        {
            List<ItemDrop.ItemData.SharedData> carried = CarriedItems.All(humanoid)
                .Select(item => item.GetComponent<ItemDrop>()).Where(drop => drop != null).Select(drop => drop.m_itemData.m_shared).ToList();
            HashSet<string> stocked = new HashSet<string>(carried.Where(ProjectileRules.IsAmmo).Select(ammo => ammo.m_ammoType));
            string? unstocked = carried.Where(ProjectileRules.TakesAmmo).Select(weapon => weapon.m_ammoType).FirstOrDefault(kind => !stocked.Contains(kind));
            if (unstocked != null)
            {
                build.Report.Warn("a bow or crossbow shoots its ammunition's projectile, and this creature carries none of its own for the swap "
                    + "to reach: give the ammunition in its gear (always)", "projectile");
            }
        }
    }
}
