using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Load
{
    /// <summary>
    /// A fake player's looks, written into its body's ZDO under the keys a player's own VisEquipment writes, so a real
    /// player near the crowd draws ordinary Vikings rather than bald ones in their underwear, and each body update is
    /// about the size of a real player's: the body model, skin and hair colours, a hair and (men) a beard from the game's
    /// own customization items, one armour set (chest, legs and, but for rags, its helmet), a cape, and a weapon and a
    /// shield on the back. An item is written only when the ObjectDB has it.
    /// </summary>
    internal static class FakeLook
    {
        private static readonly string[] Sets = { "Rags", "Leather", "TrollLeather", "Bronze", "Iron" };
        private static readonly string[] Capes = { "CapeDeerHide", "CapeTrollHide", "CapeWolf" };
        private static readonly string[] Weapons = { "AxeFlint", "SwordBronze", "MaceBronze", "SpearBronze", "AxeIron", "SwordIron" };
        private static readonly string[] Shields = { "ShieldWood", "ShieldBronzeBuckler", "ShieldBanded" };

        private static readonly Vector3[] Skins =
        {
            new Vector3(1f, 1f, 1f), new Vector3(0.9f, 0.8f, 0.72f), new Vector3(0.75f, 0.62f, 0.52f), new Vector3(0.55f, 0.42f, 0.34f),
        };

        private static readonly Vector3[] HairColours =
        {
            new Vector3(0.95f, 0.82f, 0.55f), new Vector3(0.45f, 0.3f, 0.18f), new Vector3(0.16f, 0.13f, 0.11f), new Vector3(0.7f, 0.32f, 0.16f),
        };

        private static List<int> hairs;
        private static List<int> beards;

        internal static void Dress(ZDO zdo, System.Random random)
        {
            bool woman = random.Next(2) == 1;
            zdo.Set(ZDOVars.s_modelIndex, woman ? 1 : 0);
            zdo.Set(ZDOVars.s_skinColor, Skins[random.Next(Skins.Length)]);
            zdo.Set(ZDOVars.s_hairColor, HairColours[random.Next(HairColours.Length)]);
            Pick(zdo, ZDOVars.s_hairItem, Items("Hair", ref hairs), random);
            if (!woman) Pick(zdo, ZDOVars.s_beardItem, Items("Beard", ref beards), random);
            Armour(zdo, Sets[random.Next(Sets.Length)]);
            Gear(zdo, random);
        }

        private static void Armour(ZDO zdo, string set)
        {
            Known(zdo, ZDOVars.s_chestItem, "Armor" + set + "Chest");
            Known(zdo, ZDOVars.s_legItem, "Armor" + set + "Legs");
            if (set != "Rags") Known(zdo, ZDOVars.s_helmetItem, "Helmet" + set);
        }

        // A cape, and a weapon and a shield carried on the back, at quality 1 as a player's own game writes them.
        private static void Gear(ZDO zdo, System.Random random)
        {
            if (Known(zdo, ZDOVars.s_shoulderItem, Capes[random.Next(Capes.Length)]))
            {
                zdo.Set(ZDOVars.s_shoulderItemVariant, 0);
                zdo.Set(ZDOVars.s_shoulderItemQuality, 1);
            }
            if (Known(zdo, ZDOVars.s_rightBackItem, Weapons[random.Next(Weapons.Length)])) zdo.Set(ZDOVars.s_rightBackItemQuality, 1);
            if (!Known(zdo, ZDOVars.s_leftBackItem, Shields[random.Next(Shields.Length)])) return;
            zdo.Set(ZDOVars.s_leftBackItemVariant, 0);
            zdo.Set(ZDOVars.s_leftBackItemQuality, 1);
        }

        private static void Pick(ZDO zdo, int key, List<int> from, System.Random random)
        {
            if (from.Count > 0) zdo.Set(key, from[random.Next(from.Count)]);
        }

        private static bool Known(ZDO zdo, int key, string item)
        {
            if (!ObjectDB.instance || !ObjectDB.instance.GetItemPrefab(item)) return false;
            zdo.Set(key, item.GetStableHashCode());
            return true;
        }

        // The game's customization items of one kind (Hair1, Beard3...), not the "None" ones; looked up once.
        private static List<int> Items(string kind, ref List<int> cache)
        {
            if (cache != null) return cache;
            var found = new List<int>();
            if (!ObjectDB.instance) return found;
            foreach (GameObject item in ObjectDB.instance.m_items)
            {
                if (!item || !item.name.StartsWith(kind, StringComparison.Ordinal) || item.name.EndsWith("None", StringComparison.Ordinal)) continue;
                if (Customization(item)) found.Add(item.name.GetStableHashCode());
            }
            return cache = found;
        }

        private static bool Customization(GameObject item)
        {
            ItemDrop drop = item.GetComponent<ItemDrop>();
            return drop && drop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Customization;
        }
    }
}
