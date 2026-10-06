using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// One of PackPanel's own panels under the player panel, found by name once per inventory screen and kept while it
    /// lives: the pop-ups are asked for every frame the inventory is open. That none is there is remembered too, until the
    /// panel is made (<see cref="Made"/>) or the screen is a new one.
    /// </summary>
    public sealed class PanelFind
    {
        private readonly string name;
        private InventoryGui foundIn;
        private RectTransform found;

        public PanelFind(string name) => this.name = name;

        public RectTransform In(InventoryGui gui)
        {
            if (gui == null)
                return null;
            if (ReferenceEquals(gui, foundIn) && (ReferenceEquals(found, null) || found != null))
                return found;
            foundIn = gui;
            found = gui.m_player.Find(name) as RectTransform;
            return found;
        }

        public void Made(InventoryGui gui, RectTransform panel)
        {
            foundIn = gui;
            found = panel;
        }
    }
}
