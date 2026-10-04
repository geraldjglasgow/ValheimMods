using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// The search text as a filter. A word must appear, without regard to case, in the item's name in the game's
    /// language or in its prefab name; @word in the same names of one of the materials the crafting panel lists for the
    /// recipe at the listed quality and this station (<see cref="RecipeNeeds"/>); a leading - turns either kind into a word that must not match. Every word must hold. Blank text matches all.
    /// </summary>
    public sealed class RecipeQuery
    {
        public static readonly RecipeQuery All = new RecipeQuery();

        private readonly List<string> names = new List<string>();
        private readonly List<string> materials = new List<string>();
        private readonly List<string> notNames = new List<string>();
        private readonly List<string> notMaterials = new List<string>();

        public bool Empty => names.Count + materials.Count + notNames.Count + notMaterials.Count == 0;

        public static RecipeQuery Parse(string text)
        {
            RecipeQuery query = new RecipeQuery();
            if (string.IsNullOrEmpty(text))
                return query;
            foreach (string raw in text.ToLowerInvariant().Split(' ', '\t'))
                query.Add(raw.Trim());
            return query;
        }

        private void Add(string word)
        {
            bool not = word.StartsWith("-");
            if (not)
                word = word.Substring(1);
            bool material = word.StartsWith("@");
            if (material)
                word = word.Substring(1);
            if (word.Length == 0)
                return;
            List<string> into = material ? (not ? notMaterials : materials) : (not ? notNames : names);
            into.Add(word);
        }

        /// <summary>The recipe at that quality (1 for a new craft, the next level for an upgrade).</summary>
        public bool Matches(Recipe recipe, int quality)
        {
            if (Empty)
                return true;
            if (recipe == null || recipe.m_item == null)
                return false;
            string name = ItemText(recipe.m_item);
            if (!AllIn(names, name) || AnyIn(notNames, name))
                return false;
            if (materials.Count == 0 && notMaterials.Count == 0)
                return true;
            List<string> held = MaterialTexts(recipe, quality);
            foreach (string word in materials)
            {
                if (!held.Exists(text => text.Contains(word)))
                    return false;
            }
            return !notMaterials.Exists(word => held.Exists(text => text.Contains(word)));
        }

        private static bool AllIn(List<string> words, string text) => words.TrueForAll(text.Contains);

        private static bool AnyIn(List<string> words, string text) => words.Exists(text.Contains);

        /// <summary>The materials the crafting panel would list for the recipe at this station (<see cref="RecipeNeeds"/>).</summary>
        private static List<string> MaterialTexts(Recipe recipe, int quality)
        {
            List<string> result = new List<string>();
            bool upgrader = RecipeNeeds.AtUpgrader;
            foreach (Piece.Requirement requirement in recipe.m_resources)
            {
                if (RecipeNeeds.Counts(requirement, quality, upgrader))
                    result.Add(ItemText(requirement.m_resItem));
            }
            return result;
        }

        /// <summary>The item's name in the game's language and its prefab name, lower case.</summary>
        private static string ItemText(ItemDrop item)
        {
            string shown = Language.Localize(item.m_itemData.m_shared.m_name);
            return (shown + "\n" + item.name).ToLowerInvariant();
        }
    }
}
