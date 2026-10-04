using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace OpenKeep.Recipes
{
    /// <summary>
    /// Section "12. Recipe List": the search row above the crafting panel's recipe list, favourite recipes, the list or
    /// grid view and the gamepad shortcuts. All of them change only how the player's own panel looks and reacts, so
    /// none is synced. Values are read at use time.
    /// </summary>
    public static class RecipeListSettings
    {
        public const string Section = "12. Recipe List";

        public static ConfigEntry<bool> Search { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SearchKey { get; private set; }
        public static ConfigEntry<bool> ClearSearchOnClose { get; private set; }
        public static ConfigEntry<bool> Favourites { get; private set; }
        public static ConfigEntry<bool> FavouritesFirst { get; private set; }
        public static ConfigEntry<RecipeView> View { get; private set; }
        public static ConfigEntry<bool> GamepadControls { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Search = synced.Bind(Section, "Search", true,
                "A search row above the crafting panel's recipe list. Words find recipes with every word in the item's name; @word finds recipes that need a material with that word in its name (@iron); -word leaves matches out (-@wood). The row also holds the favourites only star and the view button.", synced: false);
            SearchKey = synced.Bind(Section, "Search Key", new KeyboardShortcut(KeyCode.F, KeyCode.LeftControl),
                "With the crafting panel open: puts the cursor into the search field. Escape in the field clears it, Enter keeps the text.", synced: false);
            ClearSearchOnClose = synced.Bind(Section, "Clear Search On Close", true,
                "The search text is cleared when the inventory closes. Off: it stays until you change it.", synced: false);
            BindFavourites(synced);
            View = synced.Bind(Section, "Recipe View", RecipeView.List,
                "List: the game's list. CompactList: lower rows, more on screen. SmallGrid, MediumGrid, LargeGrid: icon tiles, 5, 4 or 3 to a row; hover a tile for its name. The view button above the list switches it too.", synced: false);
            GamepadControls = synced.Bind(Section, "Gamepad Controls", true,
                "With the crafting panel selected on a gamepad: right stick up searches (Steam's keyboard in Big Picture and on the Steam Deck), down tracks or untracks the selected recipe, left makes it a favourite or not, right switches favourites only. In a grid the left stick's left and right move between tiles.", synced: false);
            RebuildOnChange(Search, Favourites, FavouritesFirst, View);
        }

        /// <summary>An edit of the cfg (or the view button) shows at once in an open panel.</summary>
        private static void RebuildOnChange(params ConfigEntryBase[] entries)
        {
            foreach (ConfigEntryBase entry in entries)
            {
                if (entry is ConfigEntry<bool> flag)
                    flag.SettingChanged += (_, __) => RebuildQuietly();
                else if (entry is ConfigEntry<RecipeView> view)
                    view.SettingChanged += (_, __) => RebuildQuietly();
            }
        }

        /// <summary>Logged, never thrown back into the setting's change (a cfg reload must go on).</summary>
        private static void RebuildQuietly()
        {
            try
            {
                RecipeFavourites.Rebuild();
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError($"OpenKeep: rebuilding the recipe list after a setting change failed: {e}");
            }
        }

        private static void BindFavourites(SyncedConfiguration synced)
        {
            Favourites = synced.Bind(Section, "Favourites", true,
                "Middle click a recipe, press Stow's Favourite Item Key over it, or click the star beside its name to make it a favourite: it shows a star. The star above the list shows favourites only; Shift + click that star to clear every favourite.", synced: false);
            FavouritesFirst = synced.Bind(Section, "Favourites First", true,
                "Favourite recipes come first in the list, before the game's order.", synced: false);
        }
    }
}
