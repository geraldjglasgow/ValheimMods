using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// Which bosses have an attack the Portalbound portals carry, which attack it is, and which hands it is thrown
    /// with. A boss's attacks are items it holds, named in its prefab's default items; the portals carry only a
    /// projectile attack, because only a projectile can leave from somewhere else. The Elder's is `gd_king_shoot`, its
    /// "shaman attack": the stream of vines (25 of them, a tenth of a second apart) it throws at a target 15 to 50 m
    /// away - not its stomp (a blow on the ground around it), its root spawn (roots raised under the target) or its
    /// scream. Bonemass's is `bonemass_attack_throw` (asked 2026-10-04): the slime ball it lobs, one at 20 m/s under
    /// gravity, which bursts into its blobs where it lands - not its punch or its poison cloud. Only these two roll
    /// Portalbound (the user, 2026-10-04). Another boss joins by adding its prefab here with its attack items and the
    /// bones of the hands it throws with; a boss not listed never rolls it (<see cref="Supports"/>, read by the rotation).
    /// Another mod's creature joins through the API (<see cref="Registrations.PortalAttacksOf"/>): its attack items are
    /// carried like these, and its hands are the bones those items are let go from (<see cref="CreatureItems.Joints"/>).
    /// A creature with neither may still carry the aspect from a registered list; it simply never throws through a portal.
    /// </summary>
    public static class PortalAttacks
    {
        private sealed class Entry
        {
            public readonly string[] Items;
            public readonly string[] Hands;

            public Entry(string[] items, string[] hands)
            {
                Items = items;
                Hands = hands;
            }
        }

        // The Elder's hands are over 2 m long from wrist to fingertip, so its portal sits on the knuckles of the middle
        // fingers, the heart of each hand, rather than on the wrist bones. Bonemass's knuckles are a metre past its
        // wrists (`l_hand`, `r_hand`); its middle finger's are `l_hand.007` and `r_hand.007`.
        private static readonly Dictionary<string, Entry> ByBoss = new Dictionary<string, Entry>
        {
            ["gd_king"] = new Entry(new[] { "gd_king_shoot" }, new[] { "l_middle1", "r_middle1" }),
            ["Bonemass"] = new Entry(new[] { "bonemass_attack_throw" }, new[] { "l_hand.007", "r_hand.007" }),
        };

        public static bool Supports(string bossPrefab) => ItemsOf(bossPrefab) != null;

        /// <summary>True when this attack is one of the boss's portal-carried projectile attacks.</summary>
        public static bool Carries(Humanoid boss, string bossPrefab, Attack attack)
        {
            ItemDrop.ItemData? weapon = attack.GetWeapon();
            string[]? items = ItemsOf(bossPrefab);
            return weapon != null && items != null && attack.m_attackType == Attack.AttackType.Projectile
                && Array.IndexOf(items, ItemName(boss, weapon)) >= 0;
        }

        // The listed boss's own items, else the ones another mod registered for the prefab; null for neither.
        private static string[]? ItemsOf(string prefab) =>
            ByBoss.TryGetValue(prefab, out Entry entry) ? entry.Items : Registrations.PortalAttacksOf(prefab);

        // The item an attack came from: its drop prefab when the item database knows it, else the boss's own item of the
        // same name - creature attack items are not in the database, so the prefab holds the only copy.
        private static string ItemName(Humanoid boss, ItemDrop.ItemData weapon) =>
            weapon.m_dropPrefab != null ? weapon.m_dropPrefab.name : CreatureItems.NameOf(boss, weapon.m_shared.m_name);

        // The listed boss's hands, else the bones a registered creature's attack items are let go from.
        private static string[] HandsOf(Humanoid boss, string prefab)
        {
            if (ByBoss.TryGetValue(prefab, out Entry entry))
            {
                return entry.Hands;
            }
            string[]? items = Registrations.PortalAttacksOf(prefab);
            return items != null ? CreatureItems.Joints(boss, items) : Array.Empty<string>();
        }

        /// <summary>
        /// The hand the boss throws with right now: of its listed hands, the one reaching furthest out along its facing.
        /// Every machine plays the same animation, so every machine picks the same hand. Null when it has none.
        /// </summary>
        public static Transform? ThrowingHand(Character boss, string bossPrefab)
        {
            GameObject? visual = boss.GetVisual();
            if (visual == null || !(boss is Humanoid humanoid))
            {
                return null;
            }
            Transform? best = null;
            float furthest = float.MinValue;
            foreach (string name in HandsOf(humanoid, bossPrefab))
            {
                Transform? hand = Utils.FindChild(visual.transform, name);
                float reach = hand != null ? Vector3.Dot(hand.position - boss.transform.position, boss.transform.forward) : 0f;
                if (hand != null && reach > furthest)
                {
                    best = hand;
                    furthest = reach;
                }
            }
            return best;
        }
    }
}
