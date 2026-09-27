using System.Collections.Generic;
using System.Linq;
using EarthWright.Core;

namespace EarthWright.Gear
{
    /// <summary>
    /// The crafting recipes of the hoe and cultivator. The game's own requirements of the hoe and cultivator recipes are
    /// remembered the first time each recipe is seen and never changed in place, so an empty "Upgrade Cost" setting (or
    /// EarthWright switched off) always restores exactly the game's costs. The recipes found here are also the ones whose
    /// station level <see cref="StationLevelPatch"/> adjusts.
    /// </summary>
    public static class ToolRecipes
    {
        private static readonly Dictionary<Recipe, Piece.Requirement[]> originals = new Dictionary<Recipe, Piece.Requirement[]>();
        private static readonly HashSet<Recipe> tools = new HashSet<Recipe>();

        /// <summary>The recipe that makes the item with this prefab name, or null.</summary>
        public static Recipe Find(ObjectDB db, string itemPrefab)
        {
            if (db == null)
                return null;
            foreach (Recipe recipe in db.m_recipes)
            {
                if (recipe != null && recipe.m_item != null && recipe.m_item.gameObject.name == itemPrefab)
                    return recipe;
            }
            return null;
        }

        /// <summary>The requirements the recipe had when EarthWright first saw it.</summary>
        public static Piece.Requirement[] Original(Recipe recipe)
        {
            if (!originals.TryGetValue(recipe, out Piece.Requirement[] original))
            {
                original = recipe.m_resources ?? new Piece.Requirement[0];
                originals[recipe] = original;
            }
            return original;
        }

        /// <summary>Writes a game tool's upgrade cost per level: the game's own when the setting is empty or EarthWright is off.</summary>
        public static void ApplyUpgradeCost(ObjectDB db, ToolLevelSettings tool)
        {
            Recipe recipe = Find(db, tool.Prefab);
            if (recipe == null)
                return;
            tools.Add(recipe);
            Piece.Requirement[] original = Original(recipe);
            if (string.IsNullOrWhiteSpace(tool.UpgradeCost.Value) || !GeneralSettings.Enabled.Value)
            {
                recipe.m_resources = original;
                return;
            }
            List<Piece.Requirement> list = original.Select(RecipeParts.Clone).ToList();
            RecipeParts.SetPerLevel(db, list, tool.UpgradeCost.Value);
            recipe.m_resources = list.ToArray();
        }

        /// <summary>The recipe makes the hoe or the cultivator.</summary>
        public static bool IsToolRecipe(Recipe recipe)
        {
            return recipe != null && tools.Contains(recipe);
        }
    }
}
