using PackPanel.Core;

namespace PackPanel.Panels
{
    /// <summary>
    /// The inventory panel's height as it would be without the worn backpack's rows (the user's call, 2026-09-28): the
    /// stats panel and the slot panel take their height from the inventory panel, and a backpack's rows should grow only
    /// the inventory panel, not them. Each row is the game's grid row height (<c>m_invGridHeight</c>), as <see cref="PanelSize"/> adds it.
    /// </summary>
    public static class BackpackPanelHeight
    {
        public static float Of(InventoryGui gui)
        {
            int rows = InventoryState.Active ? InventoryState.Layout.BackpackRows : 0;
            return gui.m_player.sizeDelta.y - rows * gui.m_invGridHeight;
        }
    }
}
