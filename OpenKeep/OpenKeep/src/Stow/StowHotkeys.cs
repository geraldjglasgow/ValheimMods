using OpenKeep.Core;

namespace OpenKeep.Stow
{
    /// <summary>
    /// Polls the inventory hotkeys once per frame after the game's own inventory update, only while the panel is
    /// visible and no popup or split dialog is up. Combined shortcuts are checked before the single key they share
    /// (Shift+G before G, Shift+F before F, Shift+Delete before Delete); the core's single-key rule keeps them apart anyway.
    /// </summary>
    public static class StowHotkeys
    {
        /// <summary>After the inventory's update (<see cref="InventoryUpdatePatch"/>).</summary>
        public static void Tick(InventoryGui __instance)
        {
            PanelButtons.Follow(__instance);
            Poll(__instance);
        }

        public static void Poll(InventoryGui gui)
        {
            TrashMode.Tick(gui);
            if (!StowSettings.Enabled.Value || !Keys.InventoryOpen || Player.m_localPlayer == null || UnifiedPopup.IsVisible())
                return;
            if (gui.m_splitDialog != null && gui.m_splitDialog.IsActive)
                return;
            PollActions(gui);
            PollHovered(gui);
            Cycling.Poll(gui);
        }

        private static void PollActions(InventoryGui gui)
        {
            if (Keys.Pressed(StowSettings.QuickStackKey))
                StowActions.QuickStack();
            else if (Keys.Pressed(StowSettings.TakeAllKey))
                StowActions.TakeAll(gui);
            else if (Keys.Pressed(StowSettings.StoreAllKey))
                StowActions.StoreAll();
            else if (Keys.Pressed(StowSettings.TopUpKey))
                TopUp.Run();
            else if (Keys.Pressed(StowSettings.SortInventoryKey))
                Sorting.SortPlayer(Player.m_localPlayer, false);
            else if (Keys.Pressed(StowSettings.SortContainerKey))
                Sorting.SortOpenContainer();
            else if (Keys.Pressed(StowSettings.DestroyJunkKey))
                Trash.DestroyJunk(gui);
            else if (Keys.Pressed(StowSettings.TrashKey))
                Trash.TrashHovered(gui);
        }

        private static void PollHovered(InventoryGui gui)
        {
            if (!HoveredItem.Find(gui, out InventoryGrid grid, out Vector2i pos))
                return;
            bool playerGrid = grid == gui.m_playerGrid;
            ItemDrop.ItemData item = HoveredItem.Item(grid, pos);
            if (Keys.Pressed(StowSettings.FavouriteSlotKey))
            {
                if (playerGrid)
                    Favourites.ToggleSlotWithMessage(pos);
            }
            else if (item == null)
                return;
            else if (Keys.Pressed(StowSettings.FavouriteItemKey))
                Favourites.ToggleItemWithMessage(item);
            else if (Keys.Pressed(StowSettings.JunkKey))
                Favourites.ToggleJunkWithMessage(item);
            else if (Keys.Pressed(StowSettings.FindKey))
                Finder.Find(item);
            else if (playerGrid && Keys.Pressed(StowSettings.StoreOneKey))
                Routing.StoreOne(item);
        }
    }
}
