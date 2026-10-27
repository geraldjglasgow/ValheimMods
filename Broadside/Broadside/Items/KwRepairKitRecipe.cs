using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Wooden Repair Kit's recipe (<see cref="KwRepairKit"/>), in every ObjectDB the game builds or copies once the kit
    /// exists, and at once when the kit is made: one kit for <see cref="Cost"/> (Meadows things: wood and resin) at a
    /// workbench of level 1. Off when the game has no workbench or one of the items is missing, so it is never made by hand
    /// anywhere.
    /// </summary>
    [HarmonyPatch]
    public static class KwRepairKitRecipe
    {
        public static readonly (string Item, int Amount)[] Cost = { ("Wood", 4), ("Resin", 2) };
        private const string Workbench = "piece_workbench";
        private static Recipe? recipe;

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake)), HarmonyPostfix]
        private static void Awoke(ObjectDB __instance) => Guard.Run("Broadside repair kit recipe", () => Install(__instance));

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), HarmonyPostfix]
        private static void Copied(ObjectDB __instance) => Guard.Run("Broadside repair kit recipe", () => Install(__instance));

        /// <summary>The kit's recipe put in a database, once the kit exists.</summary>
        public static void Install(ObjectDB? db)
        {
            GameObject? kit = KwRepairKit.Prefab;
            if (db == null || kit == null)
                return;
            recipe ??= Make(kit);
            GameObject? bench = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Workbench) : null;
            recipe.m_craftingStation = bench != null ? bench.GetComponent<CraftingStation>() : null;
            recipe.m_resources = Requirements(db);
            recipe.m_enabled = recipe.m_craftingStation != null && recipe.m_resources.Length == Cost.Length;
            if (!recipe.m_enabled && ZNetScene.instance != null)
                Log.Warn($"Broadside: the {KwRepairKit.Label} cannot be made (no {Workbench}, or an item of its cost is missing).");
            if (!db.m_recipes.Contains(recipe))
                db.m_recipes.Add(recipe);
        }

        private static Recipe Make(GameObject kit)
        {
            Recipe made = ScriptableObject.CreateInstance<Recipe>();
            made.name = "Recipe_" + KwRepairKit.Name;
            made.m_item = kit.GetComponent<ItemDrop>();
            made.m_amount = 1;
            made.m_minStationLevel = 1;
            return made;
        }

        // The cost as the game's requirements, each item found in the database; a missing one left out.
        private static Piece.Requirement[] Requirements(ObjectDB db)
        {
            List<Piece.Requirement> needs = new List<Piece.Requirement>();
            foreach ((string item, int amount) in Cost)
            {
                ItemDrop? drop = db.GetItemPrefab(item)?.GetComponent<ItemDrop>();
                if (drop != null)
                    needs.Add(new Piece.Requirement { m_resItem = drop, m_amount = amount, m_recover = true });
            }
            return needs.ToArray();
        }
    }
}
