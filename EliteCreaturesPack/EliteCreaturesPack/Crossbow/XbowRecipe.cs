using System.Collections.Generic;
using EliteCreaturesPack.Core;
using HarmonyLib;
using UnityEngine;

namespace EliteCreaturesPack.Crossbow
{
    /// <summary>
    /// The Bone Crossbow's recipe, in every ObjectDB the game builds or copies once the item exists: at the workbench, at
    /// the settings' level, costing what the settings' `Recipe` lists (item:amount:amount per upgrade). An item name the
    /// game does not know is logged and left out. Off (and not made at all) while `Craftable` is off, or when no
    /// workbench is found: without a station the game would let it be made by hand anywhere. Rebuilt when the settings
    /// change.
    /// </summary>
    public static class XbowRecipe
    {
        private const string Workbench = "piece_workbench";

        private static Recipe? recipe;

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class Built
        {
            private static void Postfix(ObjectDB __instance) => SafeCall.Run("bone crossbow recipe", () => Install(__instance));
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        public static class Copied
        {
            private static void Postfix(ObjectDB __instance) => SafeCall.Run("bone crossbow recipe", () => Install(__instance));
        }

        public static void Install(ObjectDB? db)
        {
            if (db == null || XbowItem.Prefab == null)
            {
                return;
            }
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.name = "Recipe_" + XbowItem.PrefabName;
                recipe.m_item = XbowItem.Prefab.GetComponent<ItemDrop>();
                recipe.m_amount = 1;
            }
            Fill(db, recipe);
            if (!db.m_recipes.Contains(recipe))
            {
                db.m_recipes.Add(recipe);
            }
        }

        private static void Fill(ObjectDB db, Recipe made)
        {
            GameObject? bench = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Workbench) : null;
            made.m_craftingStation = bench != null ? bench.GetComponent<CraftingStation>() : null;
            made.m_minStationLevel = XbowItemSettings.WorkbenchLevel;
            made.m_resources = Cost(db, XbowItemSettings.Recipe);
            made.m_enabled = XbowItemSettings.Craftable && made.m_craftingStation != null && made.m_resources.Length > 0;
            if (XbowItemSettings.Craftable && made.m_craftingStation == null)
            {
                Log.Warn($"Bone crossbow: no {Workbench} found; it cannot be made.");
            }
        }

        /// <summary>"Wood:10:5, BoneFragments:12:6" into requirements; unknown items and bad entries are logged and skipped.</summary>
        private static Piece.Requirement[] Cost(ObjectDB db, string text)
        {
            var cost = new List<Piece.Requirement>();
            foreach (string entry in text.Split(','))
            {
                string[] parts = entry.Trim().Split(':');
                ItemDrop? item = parts.Length >= 2 ? db.GetItemPrefab(parts[0].Trim())?.GetComponent<ItemDrop>() : null;
                if (item == null || !int.TryParse(parts[1], out int amount))
                {
                    if (entry.Trim().Length > 0)
                    {
                        Log.Warn($"Bone crossbow recipe: \"{entry.Trim()}\" is not item:amount[:per upgrade] with an item the game knows; left out.");
                    }
                    continue;
                }
                int perLevel = parts.Length >= 3 && int.TryParse(parts[2], out int per) ? per : 0;
                cost.Add(new Piece.Requirement { m_resItem = item, m_amount = amount, m_amountPerLevel = perLevel, m_recover = true });
            }
            return cost.ToArray();
        }
    }
}
