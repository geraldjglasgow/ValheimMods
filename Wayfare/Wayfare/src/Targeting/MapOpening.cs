namespace Wayfare.Targeting
{
    /// <summary>
    /// Opens the large map for Wayfare's pickers (portals and sea gates). The game never has the inventory and the
    /// large map open together: it ignores the inventory's keys while the map is open (InventoryGui.Update) and the
    /// map's keys while the inventory is open (Minimap.Update), so a player who walked into a portal with the inventory
    /// open could close neither. So the inventory (with any chest open in it) closes first, as the game itself needs
    /// before its map opens.
    /// </summary>
    internal static class MapOpening
    {
        public static void Large(Minimap map)
        {
            if (InventoryGui.IsVisible() && InventoryGui.instance != null)
                InventoryGui.instance.Hide();
            map.SetMapMode(Minimap.MapMode.Large);
        }
    }
}
