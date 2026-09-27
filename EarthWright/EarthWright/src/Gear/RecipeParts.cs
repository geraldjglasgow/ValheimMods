using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Gear
{
    /// <summary>
    /// Turns the "Item:Amount" settings into the game's recipe requirements and finds crafting stations by prefab name.
    /// Item and station names are looked up in the object database; unknown names are skipped with one warning each.
    /// </summary>
    public static class RecipeParts
    {
        private static readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>A copy of a requirement, so the game's own recipe objects are never changed in place.</summary>
        public static Piece.Requirement Clone(Piece.Requirement r)
        {
            return new Piece.Requirement
            {
                m_resItem = r.m_resItem,
                m_amount = r.m_amount,
                m_extraAmountOnlyOneIngredient = r.m_extraAmountOnlyOneIngredient,
                m_amountPerLevel = r.m_amountPerLevel,
                m_upgraderResource = r.m_upgraderResource,
                m_recover = r.m_recover,
            };
        }

        /// <summary>The item of a prefab name, or null (warned once).</summary>
        public static ItemDrop Item(ObjectDB db, string name)
        {
            GameObject prefab = db != null ? db.GetItemPrefab(name) : null;
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null)
                WarnOnce("item", name);
            return drop;
        }

        /// <summary>The crafting station of a prefab name, found on the object database's recipes; null when no recipe uses it.</summary>
        public static CraftingStation Station(ObjectDB db, string prefabName)
        {
            if (db == null || string.IsNullOrWhiteSpace(prefabName))
                return null;
            string wanted = prefabName.Trim();
            foreach (Recipe recipe in db.m_recipes)
            {
                CraftingStation station = recipe != null ? recipe.m_craftingStation : null;
                if (station != null && Utils.GetPrefabName(station.gameObject) == wanted)
                    return station;
            }
            WarnOnce("crafting station", wanted);
            return null;
        }

        /// <summary>Crafting requirements (amount to craft, nothing per level) from "Item:Amount" pairs.</summary>
        public static List<Piece.Requirement> Crafting(ObjectDB db, string text)
        {
            List<Piece.Requirement> result = new List<Piece.Requirement>();
            foreach (KeyValuePair<string, int> pair in SettingLists.Pairs(text))
            {
                ItemDrop item = Item(db, pair.Key);
                if (item != null && pair.Value > 0)
                    result.Add(new Piece.Requirement { m_resItem = item, m_amount = pair.Value, m_amountPerLevel = 0, m_recover = true });
            }
            return result;
        }

        /// <summary>
        /// Sets the per-level amounts from "Item:Amount" pairs on a list of requirements: listed items get their amount per
        /// level (an item not yet in the list is added with nothing to craft), every other ordinary item gets none. The
        /// Refinement Forge's upgrader resources are left as they are.
        /// </summary>
        public static void SetPerLevel(ObjectDB db, List<Piece.Requirement> list, string text)
        {
            foreach (Piece.Requirement r in list)
            {
                if (!r.m_upgraderResource)
                    r.m_amountPerLevel = 0;
            }
            foreach (KeyValuePair<string, int> pair in SettingLists.Pairs(text))
            {
                ItemDrop item = Item(db, pair.Key);
                if (item == null)
                    continue;
                Piece.Requirement existing = list.Find(r => r.m_resItem == item && !r.m_upgraderResource);
                if (existing == null)
                    list.Add(existing = new Piece.Requirement { m_resItem = item, m_amount = 0, m_recover = true });
                existing.m_amountPerLevel = pair.Value;
            }
        }

        private static void WarnOnce(string kind, string name)
        {
            if (warned.Add(kind + ":" + name))
                Plugin.Log.LogWarning($"EarthWright tools: no {kind} named '{name}' in this game; it is left out.");
        }
    }
}
