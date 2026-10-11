using EliteCreaturesPack.Custom.Build;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// Projectiles a definition names, and where an attack keeps its projectile. An attack fires its
    /// <c>Attack.m_attackProjectile</c> when it is a projectile attack (the game's <c>AttackType.Projectile</c>); a bow or
    /// crossbow fires its ammunition's instead, when the ammunition has one (<c>Attack.ProjectileAttackTriggered</c>). So
    /// ammunition carrying a projectile counts as firing one too. A projectile is spawned on the attacker's owner and
    /// sent to every peer, so it must be a network prefab: the game's lookup (<see cref="PrefabLookup.Prefab"/>) is
    /// ZNetScene's. It must also be something the game can launch, a component the attack sets up (<c>IProjectile</c>:
    /// a projectile, an area effect, a spawn).
    /// </summary>
    internal static class ProjectileRules
    {
        /// <summary>The projectile prefab a definition names, or null after a <c>Fail</c> at the field.</summary>
        public static GameObject? Find(CreatureBuild build, string name, string field)
        {
            GameObject? prefab = build.Find.Prefab(name);
            if (prefab == null)
            {
                build.Report.Fail($"unknown projectile '{name}'", field);
                return null;
            }
            if (prefab.GetComponent<IProjectile>() == null)
            {
                build.Report.Fail($"'{name}' is not a projectile: it has nothing an attack can launch", field);
                return null;
            }
            return prefab;
        }

        /// <summary>Whether the item fires a projectile of its own: a projectile attack, or ammunition carrying one.</summary>
        public static bool Fires(ItemDrop.ItemData.SharedData shared) =>
            IsAmmo(shared) ? shared.m_attack?.m_attackProjectile != null : Shoots(shared.m_attack) || Shoots(shared.m_secondaryAttack);

        /// <summary>Whether the item is a projectile weapon that takes ammunition (a bow, a crossbow).</summary>
        public static bool TakesAmmo(ItemDrop.ItemData.SharedData shared) => !IsAmmo(shared) && !string.IsNullOrEmpty(shared.m_ammoType);

        /// <summary>Whether the item is ammunition.</summary>
        public static bool IsAmmo(ItemDrop.ItemData.SharedData shared) =>
            shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable;

        /// <summary>Whether the attack is a projectile attack.</summary>
        public static bool Shoots(Attack? attack) => attack != null && attack.m_attackType == Attack.AttackType.Projectile;

        /// <summary>Every projectile the item fires becomes <paramref name="projectile"/>.</summary>
        public static void Swap(ItemDrop.ItemData.SharedData shared, GameObject projectile)
        {
            if (IsAmmo(shared))
            {
                shared.m_attack.m_attackProjectile = projectile;
                return;
            }
            foreach (Attack? attack in new[] { shared.m_attack, shared.m_secondaryAttack })
            {
                if (Shoots(attack))
                {
                    attack!.m_attackProjectile = projectile;
                }
            }
        }
    }
}
