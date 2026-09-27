using UnityEngine;

namespace PlateColumn
{
    /// <summary>
    /// The game's two readouts on the player panel's right, armour above weight: each is a direct child of the panel
    /// (<c>InventoryGui.m_player</c>) holding a wood background, an icon and the text the game rewrites every frame
    /// (<c>m_armor</c>, <c>m_weight</c>). Finding them also readies them, each step safe to repeat: both are pinned to
    /// the panel's top-right corner where they stand (the game anchors armour to the right edge's middle and weight to
    /// the bottom-right corner, so bought inventory rows would pull them apart), their positions are recorded for the
    /// column before it first moves them, each icon is enlarged behind its number, and each gets a tooltip.
    /// </summary>
    internal sealed class GamePlates
    {
        private const string ArmorTopic = "Armor";
        private const string ArmorTip = "The armor of everything you wear. It reduces the damage of every hit you take.";
        private const string WeightTopic = "Carry weight";
        private const string WeightTip = "What you carry, out of the most you can carry. Over it you are encumbered and cannot run.";

        private GamePlates(RectTransform armor, RectTransform weight)
        {
            Armor = armor;
            Weight = weight;
        }

        public RectTransform Armor { get; }

        public RectTransform Weight { get; }

        public static GamePlates? Find(InventoryGui gui)
        {
            if (gui == null || gui.m_player == null || gui.m_armor == null || gui.m_weight == null)
            {
                return null;
            }
            RectTransform? armor = PlateStyle.ChildHolding(gui.m_player, gui.m_armor.transform) as RectTransform;
            RectTransform? weight = PlateStyle.ChildHolding(gui.m_player, gui.m_weight.transform) as RectTransform;
            if (armor == null || weight == null || armor == weight)
            {
                return null;
            }
            PinTopRight(armor, gui.m_player);
            PinTopRight(weight, gui.m_player);
            ColumnLayout.RecordOrigin(gui.m_player, armor, weight);
            PlateStyle.CentreIcon(armor, gui.m_armor);
            PlateStyle.CentreIcon(weight, gui.m_weight);
            PlateTips.SetIfMissing(gui, armor, ArmorTopic, ArmorTip);
            PlateTips.SetIfMissing(gui, weight, WeightTopic, WeightTip);
            return new GamePlates(armor, weight);
        }

        /// <summary>Re-anchors a plate to the panel's top-right corner, leaving it where it is.</summary>
        public static void PinTopRight(RectTransform plate, RectTransform panel)
        {
            if (plate.anchorMin == Vector2.one && plate.anchorMax == Vector2.one)
            {
                return;
            }
            Vector2 size = plate.rect.size;
            Vector2 pivot = plate.localPosition;
            plate.anchorMin = Vector2.one;
            plate.anchorMax = Vector2.one;
            plate.sizeDelta = size;
            plate.anchoredPosition = pivot - panel.rect.max;
        }
    }
}
