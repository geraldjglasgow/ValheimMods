using PlateColumn;
using PackPanel.Core;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>Armor, weight and world level in inventory order, with matching smaller squares under the minimap.</summary>
    public static class HudStats
    {
        private static Plate armor;
        private static float nextTry;

        public static void Refresh(bool show)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_takeAllButton == null) return;
            Armor(gui, show);
            GameObject smallRoot = Minimap.instance != null ? Minimap.instance.m_smallRoot : null;
            Transform small = smallRoot != null ? smallRoot.transform : null;
            RectTransform row = small != null ? small.Find("PlateColumn_hudboxes") as RectTransform : null;
            if (row == null) return;
            bool styled = InventorySettings.Enabled.Value;
            row.anchoredPosition = new Vector2(0f, styled ? -2f : -HudRow.Gap);
            row.localScale = Vector3.one * (styled ? 0.65f : HudRow.Scale);
            foreach (Transform child in row)
            {
                bool target = child.name.EndsWith("_packpanel_armor") || child.name.EndsWith("_packpanel_weight") || child.name.EndsWith("_world_tier");
                if (target) StatIconFrames.Dress(gui, (RectTransform)child, styled);
            }
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
