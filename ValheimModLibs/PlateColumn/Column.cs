using UnityEngine;

namespace PlateColumn
{
    /// <summary>
    /// The stat readouts on the inventory's player panel as one column of small brown boxes any of our mods can add to:
    /// the game's armour and weight plates, restyled, and every box a mod adds, top to bottom by rank, stacked just
    /// outside the panel's top-right corner. Each box is <see cref="BoxSize"/> square with its icon at the top and its
    /// number underneath, drawn with <see cref="Skin.Box"/>; each carries a tooltip pinned right of it.
    /// <para>
    /// Each mod merges its own copy of this library, so nothing is shared in memory: the column lives in the scene. It is
    /// one container object on the panel, <c>PlateColumn_boxes</c>, whose Unity layout group stacks its active children
    /// in sibling order; a mod's box is a child named with its rank, so every copy of this code sorts the same column from
    /// what it finds, whichever mod arranges last, and a hidden box leaves no gap. Plates an older copy of the library
    /// left on the panel are adopted into the container as boxes (the same objects, so references to them keep working),
    /// and once the game's plates are in the container older copies find no plates and change nothing.
    /// </para>
    /// </summary>
    public static class Column
    {
        public const int ArmorRank = 100;
        public const int WeightRank = 300;

        /// <summary>The gap between the player panel's right edge and the column.</summary>
        public const float Left = 8f;

        /// <summary>The edge of every box.</summary>
        public const float BoxSize = 64f;

        /// <summary>The gap between two boxes.</summary>
        public const float Spacing = 6f;

        /// <summary>How far under the player panel's top edge the first box's top sits.</summary>
        public const float Top = 6f;

        /// <summary>
        /// Builds the column if needed - the container, the game's plates and any older plates adopted and styled as
        /// boxes, tooltips on the game's two - puts every box in rank order and lays it out now. Call it again after
        /// showing or hiding a box (the column also notices on its own the same frame). False, with nothing changed,
        /// when either game plate is missing.
        /// </summary>
        public static bool Arrange(InventoryGui gui) => BoxStack.Ready(gui) != null;

        /// <summary>
        /// The container holding the boxes (<c>PlateColumn_boxes</c>, a child of <c>gui.m_player</c>, anchored to the
        /// panel's top-right corner with its pivot at its own top-left, as wide as a box and as tall as the visible
        /// boxes), after building and arranging the column as <see cref="Arrange"/> does; for a mod that places something
        /// against the column. Null when the game's plates are missing.
        /// </summary>
        public static RectTransform? Boxes(InventoryGui gui) => BoxStack.Ready(gui)?.Boxes;

        /// <summary>
        /// The mod's box for this spec - found when the column already has it, made from a copy of the armour box
        /// otherwise - with its tooltip, and the column arranged around it. <see cref="Plate.Rect"/> is the box. Null
        /// when the game's plates are missing.
        /// </summary>
        public static Plate? Add(InventoryGui gui, PlateSpec spec)
        {
            BoxStack? stack = BoxStack.Ready(gui);
            if (stack == null)
            {
                return null;
            }
            string name = ColumnLayout.NameOf(spec);
            Plate? plate = stack.Boxes.Find(name) is RectTransform existing
                ? PlateCopy.Wrap(existing)
                : PlateCopy.Make(stack.Game.Armor, spec, name);
            if (plate != null)
            {
                PlateTips.Set(gui, plate.Rect, spec.Topic, spec.Tip);
                stack.Order();
            }
            return plate;
        }

        /// <summary>
        /// Gives one of the game's own tooltips (a <c>UITooltip</c>, which follows the pointer) the box the column's
        /// tips show: the game's inventory item tooltip with the gold border. For a mod whose tips live elsewhere, such
        /// as the skills panel, so every tip reads alike. The game still decides when and where it shows. False, with
        /// nothing changed, when the item tooltip cannot be found.
        /// </summary>
        public static bool DressTooltip(InventoryGui gui, UITooltip tooltip)
        {
            GameObject? template = TipTemplate.For(gui);
            if (template == null || tooltip == null)
            {
                return false;
            }
            tooltip.m_tooltipPrefab = template;
            return true;
        }
    }
}
