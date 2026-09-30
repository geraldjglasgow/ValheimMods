using PlateColumn;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// Armor, weight and world level in inventory order, with matching squares in a column right of the minimap: full
    /// size (48 units, the user found them tiny at 65%), the map moved over to make room (<see cref="HudRoom"/>).
    /// </summary>
    public static class HudStats
    {
        /// <summary>The HUD column's scale: its squares as big as the stats panel's.</summary>
        private const float Scale = 1f;

        private static Plate armor;
        private static float nextTry;

        public static void Refresh(bool show)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_takeAllButton == null) return;
            Armor(gui, show);
            RectTransform column = HudRow.Container();
            if (column != null)
            {
                if (column.localScale.x != Scale) column.localScale = Vector3.one * Scale;
                foreach (Transform child in column)
                {
                    bool target = child.name.EndsWith("_packpanel_armor") || child.name.EndsWith("_packpanel_weight") || child.name.EndsWith("_world_tier");
                    if (target) StatIconFrames.Dress(gui, (RectTransform)child, true);
                }
            }
            HudRoom.Fit(column);
        }

        private static void Armor(InventoryGui gui, bool show)
        {
            if ((armor == null || armor.Rect == null) && show && Time.time >= nextTry)
            {
                nextTry = Time.time + 1f;
                // HudRow copies the game's armor plate; a null sprite preserves its shield icon.
                armor = HudRow.Add(gui, new PlateSpec("packpanel_armor", Column.ArmorRank, null, true, "", ""));
            }
            if (armor == null || armor.Rect == null) return;
            if (armor.Rect.gameObject.activeSelf != show) armor.Rect.gameObject.SetActive(show);
            if (show && armor.Text != null)
            {
                string value = Mathf.CeilToInt(Player.m_localPlayer.GetBodyArmor()).ToString();
                if (armor.Text.text != value) armor.Text.text = value;
            }
        }
    }
}
