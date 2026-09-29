using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlateColumn
{
    /// <summary>
    /// Small boxes on the HUD, in a row right under the minimap, for readouts a player wants in view without opening the
    /// inventory (the user asked for the world tier and the weight). A HUD box is made like a column box - a copy of the
    /// column's restyled armour box with the mod's icon and an empty number line - but lives in the HUD row
    /// (<see cref="HudContainer"/>), takes no pointer and has no tooltip. The row hides with the small map (the big map
    /// open, a world without a map, the HUD hidden). Boxes sort left to right by rank, named as column boxes are, so every
    /// copy of this library reads the same order. The game writes nothing here: the mod writes its box's text itself,
    /// every frame or when the value changes, and hides the box with <c>SetActive(false)</c>; the row closes the gap.
    /// </summary>
    public static class HudRow
    {
        /// <summary>The row's size against the column: a 64 unit box shows at about 45, a quarter of the minimap's width.</summary>
        public const float Scale = 0.7f;

        /// <summary>Units between the minimap's bottom edge and the top of the row.</summary>
        public const float Gap = 6f;

        /// <summary>
        /// The mod's HUD box for this spec (its tooltip words are unused), found when the row already has it, made
        /// otherwise, with the row put in rank order. Null while there is no minimap or the inventory's plates are missing.
        /// </summary>
        public static Plate? Add(InventoryGui gui, PlateSpec spec)
        {
            RectTransform? row = HudContainer.Get();
            BoxStack? stack = row != null ? BoxStack.Ready(gui) : null;
            if (row == null || stack == null)
            {
                return null;
            }
            string name = ColumnLayout.NameOf(spec);
            Plate? plate = row.Find(name) is RectTransform existing ? PlateCopy.Wrap(existing) : Make(stack, spec, name, row);
            if (plate != null)
            {
                Order(row, stack.Game);
            }
            return plate;
        }

        /// <summary>The icon of the game's weight box, for a mod that shows the weight on the HUD; null when it is missing.</summary>
        public static Sprite? WeightIcon(InventoryGui gui)
        {
            GamePlates? game = GamePlates.Find(gui);
            Plate? weight = game != null ? PlateCopy.Wrap(game.Weight) : null;
            return weight != null ? weight.Icon.sprite : null;
        }

        private static Plate? Make(BoxStack stack, PlateSpec spec, string name, RectTransform row)
        {
            Plate? plate = PlateCopy.Make(stack.Game.Armor, spec, name, row);
            if (plate != null)
            {
                foreach (Graphic graphic in plate.Rect.GetComponentsInChildren<Graphic>(true))
                {
                    graphic.raycastTarget = false;
                }
            }
            return plate;
        }

        private static void Order(RectTransform row, GamePlates game)
        {
            List<RectTransform> members = ColumnLayout.Sorted(row, game);
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].GetSiblingIndex() != i)
                {
                    members[i].SetSiblingIndex(i);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        }
    }
}
