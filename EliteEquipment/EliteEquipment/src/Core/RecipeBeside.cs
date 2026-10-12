using System.Collections.Generic;
using UnityEngine;

using Requirement = Piece.Requirement;

namespace EliteEquipment.Core
{
    /// <summary>One material of a recipe: the game item, the amount to craft, the amount per upgrade level.</summary>
    public readonly struct RecipePart
    {
        public readonly string Item;
        public readonly int Amount;
        public readonly int PerLevel;

        public RecipePart(string item, int amount, int perLevel)
        {
            Item = item;
            Amount = amount;
            PerLevel = perLevel;
        }
    }

    /// <summary>
    /// A recipe of the mod's that sits beside a game item's: the same station, station level, repair station, list order
    /// and upgrade kits, its own materials. Enabled while the feature is on and the game item's recipe is; never when the
    /// game lacks the item's station (without one it could be made by hand).
    /// </summary>
    public static class RecipeBeside
    {
        /// <summary>The game's recipe for an item prefab in this database; null when it has none.</summary>
        public static Recipe Find(ObjectDB db, string item) =>
            db.m_recipes.Find(r => r != null && r.m_item != null && r.m_item.name == item);

        public static Recipe Create(GameObject item)
        {
            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = "Recipe_" + item.name;
            recipe.m_item = item.GetComponent<ItemDrop>();
            recipe.m_amount = 1;
            return recipe;
        }

        /// <summary>Fills the recipe from the game's one beside it, and adds it to the database if it is not there.</summary>
        public static void Install(ObjectDB db, Recipe recipe, Recipe beside, RecipePart[] cost, bool on)
        {
            recipe.m_craftingStation = beside.m_craftingStation;
            recipe.m_repairStation = beside.m_repairStation;
            recipe.m_minStationLevel = beside.m_minStationLevel;
            recipe.m_listSortWeight = beside.m_listSortWeight;
            recipe.m_resources = Cost(db, cost, beside, recipe.name);
            recipe.m_enabled = on && beside.m_enabled && beside.m_craftingStation != null;
            if (!db.m_recipes.Contains(recipe))
                db.m_recipes.Add(recipe);
        }

        /// <summary>The materials found in this database, then a copy of each of the game recipe's upgrade kits.</summary>
        private static Requirement[] Cost(ObjectDB db, RecipePart[] parts, Recipe beside, string name)
        {
            var cost = new List<Requirement>();
            foreach (RecipePart part in parts)
            {
                ItemDrop item = db.GetItemPrefab(part.Item)?.GetComponent<ItemDrop>();
                if (item != null)
                    cost.Add(new Requirement { m_resItem = item, m_amount = part.Amount, m_amountPerLevel = part.PerLevel, m_recover = true });
                else
                    Plugin.Log.LogWarning($"EliteEquipment: the game has no {part.Item}; {name} costs none");
            }
            foreach (Requirement kit in beside.m_resources ?? new Requirement[0])
            {
                if (kit != null && kit.m_resItem != null && kit.m_upgraderResource)
                    cost.Add(new Requirement { m_resItem = kit.m_resItem, m_amount = kit.m_amount, m_amountPerLevel = kit.m_amountPerLevel, m_upgraderResource = true, m_recover = kit.m_recover });
            }
            return cost.ToArray();
        }
    }
}
