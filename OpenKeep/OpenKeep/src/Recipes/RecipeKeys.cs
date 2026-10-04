using System.Collections.Generic;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// A recipe's identity in the character's saved lists (favourites, the tracker): the recipe asset's name
    /// (<c>Recipe_SwordIron</c>), or its item's prefab name when the recipe has none, without the lists' separators
    /// (comma and bar). <see cref="Find"/> turns a key back into the loaded recipe; the index is rebuilt whenever the
    /// item database or its recipe count changes, so recipes other mods add later are found too.
    /// </summary>
    public static class RecipeKeys
    {
        private const int KeepAtMost = 4096;

        private static readonly Dictionary<string, Recipe> byKey = new Dictionary<string, Recipe>();
        private static readonly Dictionary<Recipe, string> keys = new Dictionary<Recipe, string>();
        private static ObjectDB indexed;
        private static int indexedCount = -1;

        /// <summary>Kept per recipe: the panel asks every frame, and reading a Unity object's name makes a new string.</summary>
        public static string Of(Recipe recipe)
        {
            if (recipe == null)
                return "";
            if (keys.TryGetValue(recipe, out string known))
                return known;
            if (keys.Count >= KeepAtMost)
                keys.Clear();
            string name = !string.IsNullOrEmpty(recipe.name) ? recipe.name : recipe.m_item != null ? recipe.m_item.name : "";
            string key = name.Replace(",", "").Replace("|", "").Trim();
            keys[recipe] = key;
            return key;
        }

        /// <summary>The loaded recipe with that key, or null (unknown, or its item is gone).</summary>
        public static Recipe Find(string key)
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null || string.IsNullOrEmpty(key))
                return null;
            if (db != indexed || db.m_recipes.Count != indexedCount)
                Index(db);
            return byKey.TryGetValue(key, out Recipe recipe) && recipe != null && recipe.m_item != null ? recipe : null;
        }

        /// <summary>The first recipe of a key wins, as the game's own lookups take the first match.</summary>
        private static void Index(ObjectDB db)
        {
            byKey.Clear();
            indexed = db;
            indexedCount = db.m_recipes.Count;
            foreach (Recipe recipe in db.m_recipes)
            {
                if (recipe == null || recipe.m_item == null)
                    continue;
                string key = Of(recipe);
                if (key.Length > 0 && !byKey.ContainsKey(key))
                    byKey[key] = recipe;
            }
        }
    }
}
