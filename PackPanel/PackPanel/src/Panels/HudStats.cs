using PackPanel.Core;
using PlateColumn;
using TMPro;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// Armor, weight and world level in inventory order, with matching squares in a column right of the minimap: full
    /// size (48 units, the user found them tiny at 65%), the map moved over to make room (<see cref="HudRoom"/>). Every
    /// frame: the column is found once and kept while it lives, and only its shown boxes are dressed.
    /// </summary>
    public static class HudStats
    {
        /// <summary>The HUD column's scale: its squares as big as the stats panel's.</summary>
        private const float Scale = 1f;

        private static Plate armor;
        private static float nextTry;
        private static int armorShown;
        private static TMP_Text armorText;
        private static RectTransform column;

        public static void Refresh(bool show)
        {
            InventoryGui gui = InventoryGui.instance;
            if (gui == null || gui.m_takeAllButton == null) return;
            Armor(gui, show);
            RectTransform hud = HudColumn();
            if (hud != null && hud.gameObject.activeInHierarchy)
            {
                if (hud.localScale.x != Scale) hud.localScale = Vector3.one * Scale;
                for (int i = 0; i < hud.childCount; i++)
                {
                    RectTransform box = hud.GetChild(i) as RectTransform;
                    if (box != null && box.gameObject.activeSelf && StatIconFrames.IsHudStat(box)) StatIconFrames.Dress(gui, box, true);
                }
            }
            HudRoom.Fit(hud);
        }

        /// <summary>The library's HUD column (<see cref="HudRow.Container"/>, which searches the minimap for it), kept while it lives.</summary>
        private static RectTransform HudColumn()
        {
            if (!Column.Active) return null;
            if (column == null) column = HudRow.Container();
            return column;
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
                // The number is turned into text only when it or the box changes.
                int value = Mathf.CeilToInt(Player.m_localPlayer.GetBodyArmor());
                if (value == armorShown && armor.Text == armorText) return;
                armor.Text.text = NumberText.Of(value);
                armorShown = value;
                armorText = armor.Text;
            }
        }
    }
}
