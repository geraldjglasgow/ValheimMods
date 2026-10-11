using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// The AI values a weapon needs in a human's hands. The game's own weapons (and the player's fists) still carry the
    /// item defaults, 2 m, 0 m, 2 s and 5 degrees, since no AI ever held them: a sword swung from 2 m mostly misses and a
    /// bow walked up to 2 m is no bow. Whatever the human carries (the bare kit, a definition's gear, its new attacks)
    /// is fitted as it is armed (<see cref="FitAll"/>, at Humanoid.Start, on every peer), by one rule:
    /// <list type="bullet">
    /// <item>A weapon whose range, minimum range and interval are all still the item defaults was never set up for an AI:
    /// those three are fitted from its own attack, and its angle too while it is still the default 5 degrees.</item>
    /// <item>A weapon with any of the three of its own (a creature's attack item, or a definition's attack that sets
    /// `max range`, `min range` or `cooldown`) keeps all its values. So a definition that sets one of them on a player's
    /// weapon sets the others it cares about with it; one that sets only `angle` keeps that angle and is fitted
    /// otherwise.</item>
    /// </list>
    /// The fitted values:
    /// <list type="bullet">
    /// <item>Melee: it stands at three quarters of its swing's reach, as the Draugr's axe does (1.6 m of 2.2), at least
    /// 1 m; a swing every 2 s, every 3 s two-handed (the Draugr's axe: 3 s); within 10 degrees of facing.</item>
    /// <item>Ranged (projectile attacks: bows, crossbows, staffs): from 20 m, the Draugr archer's range; not within 4 m,
    /// where a melee weapon or the fists take over (Humanoid.EquipBestWeapon skips a weapon inside its minimum); 1.5 s
    /// after the last shot, since the bow's draw or the crossbow's reload paces it; within 10 degrees.</item>
    /// </list>
    /// The values are written into the human's own copy of the item's shared data, never the game's: every item
    /// instantiated into an inventory has its own copy, and one that still shares its prefab's is given one first.
    /// </summary>
    internal static class HumanWeaponAi
    {
        private const float UnsetRange = 2f, UnsetInterval = 2f, UnsetAngle = 5f;
        private const float Reach = 0.75f, ReachMin = 1f, SwingAngle = 10f, OneHandPause = 2f, TwoHandPause = 3f;
        private const float ShotRange = 20f, ShotRangeMin = 4f, ShotPause = 1.5f, ShotAngle = 10f;

        private static readonly MethodInfo Clone = AccessTools.Method(typeof(object), "MemberwiseClone");

        /// <summary>Every weapon in a human's inventory, as it is armed (on every peer; only the owner's AI reads them).</summary>
        public static void FitAll(Humanoid human)
        {
            foreach (ItemDrop.ItemData item in human.GetInventory().GetAllItems())
            {
                if (item.IsWeapon() && Unset(item.m_shared))
                {
                    Own(item);
                    Fit(item.m_shared);
                }
            }
        }

        /// <summary>Fits shared data that is already the human's own (see <see cref="Copy"/>).</summary>
        public static void Fit(ItemDrop.ItemData.SharedData shared)
        {
            if (!Unset(shared))
            {
                return;
            }
            bool shot = shared.m_attack.m_attackType == Attack.AttackType.Projectile || shared.m_attack.m_attackType == Attack.AttackType.TriggerProjectile;
            if (shared.m_aiAttackMaxAngle == UnsetAngle)
            {
                shared.m_aiAttackMaxAngle = shot ? ShotAngle : SwingAngle;
            }
            if (shot)
            {
                (shared.m_aiAttackRange, shared.m_aiAttackRangeMin, shared.m_aiAttackInterval) = (ShotRange, ShotRangeMin, ShotPause);
                return;
            }
            shared.m_aiAttackRange = Mathf.Max(ReachMin, shared.m_attack.m_attackRange * Reach);
            shared.m_aiAttackInterval = TwoHanded(shared) ? TwoHandPause : OneHandPause;
        }

        /// <summary>A shallow copy of shared data: enough, since only its own number fields are written.</summary>
        public static ItemDrop.ItemData.SharedData Copy(ItemDrop.ItemData.SharedData shared) =>
            (ItemDrop.ItemData.SharedData)Clone.Invoke(shared, null);

        private static bool Unset(ItemDrop.ItemData.SharedData shared) =>
            shared.m_aiAttackRange == UnsetRange && shared.m_aiAttackRangeMin == 0f && shared.m_aiAttackInterval == UnsetInterval;

        private static bool TwoHanded(ItemDrop.ItemData.SharedData shared) =>
            shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft;

        /// <summary>Gives the item its own shared data if it still shares its prefab's.</summary>
        private static void Own(ItemDrop.ItemData item)
        {
            GameObject? prefab = item.m_dropPrefab;
            ItemDrop? drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop != null && ReferenceEquals(drop.m_itemData.m_shared, item.m_shared))
            {
                item.m_shared = Copy(item.m_shared);
            }
        }
    }
}
