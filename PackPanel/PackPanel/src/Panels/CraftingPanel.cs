using PackPanel.Core;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// A roomier crafting panel (asked 2026-10-04: "its really crowded ... we can make crafting panel larger in pack
    /// panel"). The game's panel is 570 x 650, anchored to the screen's top right; Crafting Panel Width and Height add to
    /// it, growing left and down, and the panel above it (the name, skills, trophies and PvP, the same 570 wide in the
    /// game, its contents centred) widens with it so their edges line up (asked the same day). The room goes to its parts: the recipe list takes 40% of the width (its scroll view
    /// and the row template with it, the row's centre-anchored marks kept over the icon), the description the rest, both
    /// the full height; the tabs stay at the left above the list and their line spans the panel; the item's name widens;
    /// the description text starts a little lower, so the buttons under the name (OpenKeep's Track and favourite) have
    /// room. The requirements and the Craft row are anchored to the description's bottom and follow by themselves. The
    /// width never reaches the inventory's panels on the left (the player panel and PackPanel's slot and stats panels,
    /// less the repair button that hangs off the crafting panel's left edge), the height never the screen's bottom: on a
    /// narrow screen the panel gets what fits. Everything is set from the game's own values each time, so 0, or
    /// PackPanel off, is the game's panel exactly; so is AAA Crafting installed (<see cref="AaaCraftingLink"/>), which
    /// lays the panel out itself.
    /// </summary>
    public static class CraftingPanel
    {
        private const float ListShare = 0.4f;
        private const float RepairOverhang = 80f;
        private const float Margin = 12f;
        private const float BottomMargin = 70f;
        private const float HeaderGap = 14f;

        private static CraftingParts parts;
        private static InventoryGui laidOut;

        /// <summary>At InventoryGui.Show, before the game builds the recipe list, and when a size setting changes.</summary>
        public static void Apply(InventoryGui gui)
        {
            if (gui == null || gui.m_crafting == null)
                return;
            if (laidOut != gui || parts == null || !parts.Alive)
            {
                laidOut = gui;
                parts = CraftingParts.Find(gui);
            }
            if (parts == null)
                return;
            bool on = InventoryState.Active && !AaaCraftingLink.Present;
            float width = on ? Mathf.Min(InventorySettings.CraftingWidth.Value, RoomLeft(gui)) : 0f;
            float height = on ? Mathf.Min(InventorySettings.CraftingHeight.Value, RoomBelow(gui)) : 0f;
            Lay(gui, Mathf.Round(Mathf.Max(0f, width)), Mathf.Round(Mathf.Max(0f, height)));
        }

        private static void Lay(InventoryGui gui, float width, float height)
        {
            float list = Mathf.Round(width * ListShare);
            float description = width - list;
            parts.Panel.Set(Vector2.zero, new Vector2(width, height));
            parts.Info?.Set(Vector2.zero, new Vector2(width, 0f));
            parts.Tabs?.Set(new Vector2(-width / 2f, 0f), Vector2.zero);
            parts.TabLine?.Set(new Vector2(width / 2f, 0f), new Vector2(width, 0f));
            parts.List.Set(Vector2.zero, new Vector2(list, height));
            parts.View.Set(Vector2.zero, new Vector2(list, 0f));
            parts.LayRow(list);
            parts.Description.Set(Vector2.zero, new Vector2(description, height));
            parts.Name?.Set(new Vector2(description / 2f, 0f), new Vector2(description, 0f));
            float gap = Mathf.Min(HeaderGap, height);
            parts.Text?.Set(new Vector2(0f, -(gap + height) / 2f), new Vector2(0f, height - gap));
            gui.m_recipeListBaseSize = parts.BaseSize + height;
        }

        /// <summary>
        /// Width free left of the panel, in the screen root's units: the panel's left edge at the game's width, less the
        /// repair button's overhang and a margin, less the right edge of the player panel and of every panel on it.
        /// </summary>
        private static float RoomLeft(InventoryGui gui)
        {
            RectTransform panel = gui.m_crafting;
            float left = panel.localPosition.x - panel.pivot.x * parts.Panel.Size.x;
            RectTransform player = gui.m_player;
            float right = player.localPosition.x + player.rect.xMax;
            foreach (Transform child in player)
            {
                if (child.gameObject.activeSelf && child is RectTransform rect)
                    right = Mathf.Max(right, player.localPosition.x + rect.localPosition.x + rect.rect.xMax);
            }
            return left - RepairOverhang - Margin - right;
        }

        /// <summary>Height free below the panel at the game's height, keeping clear of the key hints at the bottom.</summary>
        private static float RoomBelow(InventoryGui gui)
        {
            RectTransform panel = gui.m_crafting;
            RectTransform root = panel.parent as RectTransform;
            if (root == null)
                return 0f;
            float top = panel.localPosition.y + (1f - panel.pivot.y) * parts.Panel.Size.y;
            return top - parts.Panel.Size.y - root.rect.yMin - BottomMargin;
        }
    }
}
