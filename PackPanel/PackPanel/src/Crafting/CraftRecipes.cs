using System.Collections.Generic;
using HarmonyLib;
using PackPanel.Backpacks;
using PackPanel.Core;
using PackPanel.Tackle;
using UnityEngine;

namespace PackPanel.Crafting
{
    /// <summary>
    /// One recipe per crafted item (the backpacks and the tackleboxes), added to every ObjectDB the game builds or copies
    /// once the items exist, and brought up to date whenever an item file or a switch changes (a server's values arriving
    /// included): the station (found through the game's own recipes, so it is the same station object as theirs; an
    /// unknown name falls back to the item's default), its level, the cost (<see cref="CostText"/>; a cost with nothing
    /// usable falls back to the default) and whether it can be crafted at all.
    /// </summary>
    public static class CraftRecipes
    {
        private static readonly Dictionary<CraftedKind, Recipe> recipes = new Dictionary<CraftedKind, Recipe>();

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class Built
        {
            [HarmonyPostfix]
            public static void Postfix(ObjectDB __instance) => Install(__instance);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        public static class Copied
        {
            [HarmonyPostfix]
            public static void Postfix(ObjectDB __instance) => Install(__instance);
        }

        /// <summary>Every crafted kind, backpacks first.</summary>
        public static IEnumerable<CraftedKind> All
        {
            get
            {
                foreach (BackpackKind kind in BackpackCatalog.All)
                    yield return kind;
                foreach (TackleboxKind kind in TackleboxCatalog.All)
                    yield return kind;
            }
        }

        public static void Watch()
        {
            BackpackSettings.Enabled.SettingChanged += (sender, args) => RefreshAll();
            TackleboxSettings.Enabled.SettingChanged += (sender, args) => RefreshAll();
            InventorySettings.Enabled.SettingChanged += (sender, args) => RefreshAll();
            InventorySettings.BackpackSlot.SettingChanged += (sender, args) => RefreshAll();
        }

        public static void Install(ObjectDB db)
        {
            if (db == null)
                return;
            foreach (CraftedKind kind in All)
            {
                if (kind.Item == null)
                    continue;
                Recipe recipe = RecipeOf(kind);
                if (!db.m_recipes.Contains(recipe))
                    db.m_recipes.Add(recipe);
            }
            Refresh(db);
        }

        public static void RefreshAll() => Refresh(ObjectDB.instance);

        private static void Refresh(ObjectDB db)
        {
            if (db == null)
                return;
            foreach (KeyValuePair<CraftedKind, Recipe> pair in recipes)
                Refresh(db, pair.Key, pair.Value);
        }

        private static Recipe RecipeOf(CraftedKind kind)
        {
            if (recipes.TryGetValue(kind, out Recipe recipe))
                return recipe;
            recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = "Recipe_" + kind.Id;
            recipe.m_item = kind.Item.GetComponent<ItemDrop>();
            recipe.m_amount = 1;
            recipe.m_listSortWeight = 100 + recipes.Count;
            recipes[kind] = recipe;
            return recipe;
        }

        /// <summary>No station found means no recipe: without one the game would let it be made by hand anywhere.</summary>
        private static void Refresh(ObjectDB db, CraftedKind kind, Recipe recipe)
        {
            CraftingStation station = Station(db, kind.Recipe.Station) ?? Station(db, kind.DefaultRecipe.Station);
            recipe.m_enabled = kind.Craftable && station != null;
            recipe.m_craftingStation = station;
            recipe.m_minStationLevel = kind.Recipe.Level;
            Piece.Requirement[] cost = CostText.Parse(db, kind.Recipe.Cost, kind.Name);
            recipe.m_resources = cost.Length > 0 ? cost : CostText.Parse(db, kind.DefaultRecipe.Cost, kind.Name);
            if (station == null)
                Plugin.Log.LogWarning($"{kind.Name}: no crafting station named \"{kind.Recipe.Station}\"; it cannot be crafted");
        }

        private static CraftingStation Station(ObjectDB db, string name)
        {
            foreach (Recipe other in db.m_recipes)
            {
                if (other != null && other.m_craftingStation != null && other.m_craftingStation.name == name)
                    return other.m_craftingStation;
            }
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            return prefab != null ? prefab.GetComponent<CraftingStation>() : null;
        }
    }
}
