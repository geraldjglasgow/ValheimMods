using System.Collections.Generic;
using EliteCrafting.Items;

namespace EliteCrafting.Commands
{
    /// <summary>Recipe lookups for commands, through the spine's <c>RecipeIndex</c>.</summary>
    internal static class Recipes
    {
        /// <summary>Brings the recipe index up to date, dropping ItemTier's cache with it (<see cref="ItemTier.RefreshRecipes"/>).</summary>
        public static void Refresh() => ItemTier.RefreshRecipes();

        public static IReadOnlyList<Recipe> For(string prefab) => RecipeIndex.For(prefab);
    }
}
