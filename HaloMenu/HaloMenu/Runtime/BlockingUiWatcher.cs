namespace HaloMenu.Runtime
{
    /// <summary>Whether any vanilla UI is up that should close/refuse a ring: inventory, map, console, chat,
    /// text input, the game menu, or the local player being dead. Checked at open time and once per frame while a
    /// ring is open - a handful of cheap static reads, not part of the geometry/selection per-frame budget.</summary>
    public static class BlockingUiWatcher
    {
        public static bool IsBlocked()
        {
            if (InventoryGui.IsVisible())
                return true;
            if (Minimap.IsOpen())
                return true;
            if (Console.IsVisible())
                return true;
            if (TextInput.IsVisible())
                return true;
            if (Menu.IsVisible())
                return true;
            if (Chat.instance != null && Chat.instance.HasFocus())
                return true;
            if (Player.m_localPlayer != null && Player.m_localPlayer.IsDead())
                return true;
            return false;
        }
    }
}
