using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using Requirement = Piece.Requirement;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A recipe per caster item, beside the game item it copies (same station, station level, repair station and order),
    /// costing its own materials (<see cref="CasterGear"/>); the spell items with a recipe (Hearthsong) beside the club.
    /// In every database the game builds or copies once the items exist. Enabled while the switch is on and the game's own
    /// recipe is; an item whose game recipe or station is missing is never enabled. A recipe's cost is written once, when
    /// it is made (EliteEquipment's boots keep the Deerhide Trousers' whole cost and swap in four fifths while split), and
    /// these run before other mods' database postfixes so the boots find the trousers' recipe.
    /// </summary>
    public static class CasterRecipes
    {
        private static readonly Dictionary<string, Recipe> made = new Dictionary<string, Recipe>();

        private static readonly GearCost[] HearthsongCost =
            { new GearCost("Raspberry", 5, 0), new GearCost("Honey", 2, 0), new GearCost("Dandelion", 3, 0) };

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabasePatch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.High)]
            private static void Postfix(ObjectDB __instance) => Install(__instance);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
        private static class CopyPatch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.High)]
            private static void Postfix(ObjectDB __instance) => Install(__instance);
        }

        /// <summary>The switch changed: the live database's recipes follow it.</summary>
        public static void Refresh() => Install(ObjectDB.instance);

        public static void Install(ObjectDB db)
        {
            if (db == null)
                return;
            foreach (GearPiece piece in CasterGear.All)
                Add(db, piece.Item, piece.RecipeBeside, piece.Cost);
            GameObject hearthsong = db.GetItemPrefab(Spells.Hearthsong.Item);
            Add(db, hearthsong, CasterGear.Wand.RecipeBeside, HearthsongCost);
            foreach (FoodPiece food in CasterFood.All)
                Add(db, food.Item, food.RecipeBeside, food.Cost, food.Makes);
        }

        private static void Add(ObjectDB db, GameObject item, string beside, GearCost[] cost, int makes = 1)
        {
            Recipe game = item != null ? db.m_recipes.Find(r => r != null && r.m_item != null && r.m_item.name == beside) : null;
            if (game == null)
                return;
            Recipe recipe = RecipeOf(item);
            recipe.m_amount = makes;
            recipe.m_craftingStation = game.m_craftingStation;
            recipe.m_repairStation = game.m_repairStation;
            recipe.m_minStationLevel = game.m_minStationLevel;
            recipe.m_listSortWeight = game.m_listSortWeight;
            if (recipe.m_resources == null || recipe.m_resources.Length == 0)
                recipe.m_resources = Cost(db, item.name, cost);
            recipe.m_enabled = CastersSettings.On && game.m_enabled && game.m_craftingStation != null;
            if (!db.m_recipes.Contains(recipe))
                db.m_recipes.Add(recipe);
        }

        private static Recipe RecipeOf(GameObject item)
        {
            if (made.TryGetValue(item.name, out Recipe recipe))
                return recipe;
            recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = "Recipe_" + item.name;
            recipe.m_item = item.GetComponent<ItemDrop>();
            recipe.m_amount = 1;
            made[item.name] = recipe;
            return recipe;
        }

        private static Requirement[] Cost(ObjectDB db, string item, GearCost[] cost)
        {
            var list = new List<Requirement>();
            foreach (GearCost part in cost)
            {
                ItemDrop material = db.GetItemPrefab(part.Item)?.GetComponent<ItemDrop>();
                if (material != null)
                    list.Add(new Requirement { m_resItem = material, m_amount = part.Amount, m_amountPerLevel = part.PerLevel, m_recover = true });
                else
                    Plugin.Log.LogWarning($"EliteEquipment: the game has no {part.Item}; {item} costs none");
            }
            return list.ToArray();
        }
    }
}
