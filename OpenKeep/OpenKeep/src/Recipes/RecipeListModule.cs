using SyncedConfig;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// Entry point of the Recipe List module: a search row above the crafting panel's recipe list, favourite recipes,
    /// list and grid views and gamepad shortcuts. Binds section "12. Recipe List" (all unsynced) and its words; the
    /// patches are in <see cref="RecipeListPatches"/>. Favourites live in the character's custom data.
    /// </summary>
    public static class RecipeListModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            RecipeListSettings.Bind(synced);
            RecipeWords.Register();
        }
    }
}
