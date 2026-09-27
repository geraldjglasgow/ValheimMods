using UnityEngine;

namespace PlateColumn
{
    /// <summary>
    /// The stat plates on the inventory's player panel as one column any of our mods can add to: the game's armour and
    /// weight plates and every plate a mod adds, top to bottom by rank, evenly spaced and centred where the game's two
    /// plates sit, and never closer than a few pixels apart - so a third and a fourth plate push the column up and down
    /// rather than overlapping. Every plate carries a tooltip. Each mod merges its own copy of this library, so nothing
    /// is shared in memory: the column lives in the scene. A mod's plate is a child of the panel whose name carries its
    /// rank, and the game's own plate positions are kept on hidden markers the first arrangement leaves on the panel, so
    /// every copy of this code builds the same column from what it finds, whichever mod arranges last.
    /// </summary>
    public static class Column
    {
        public const int ArmorRank = 100;
        public const int WeightRank = 300;

        /// <summary>
        /// Pins, restyles and tips the game's two plates and lays the column out. Call it again after showing or hiding a
        /// plate: a hidden plate leaves no gap. False, with nothing changed, when either game plate is missing.
        /// </summary>
        public static bool Arrange(InventoryGui gui)
        {
            GamePlates? game = GamePlates.Find(gui);
            if (game == null)
            {
                return false;
            }
            ColumnLayout.Apply(gui.m_player, game);
            return true;
        }

        /// <summary>
        /// The mod's plate for this spec - found when this panel already has it, made from a copy of the armour plate
        /// otherwise - with its tooltip, and the column arranged around it. Null when the game's plates are missing.
        /// </summary>
        public static Plate? Add(InventoryGui gui, PlateSpec spec)
        {
            GamePlates? game = GamePlates.Find(gui);
            if (game == null)
            {
                return null;
            }
            string name = ColumnLayout.NameOf(spec);
            Plate? plate = gui.m_player.Find(name) is RectTransform existing
                ? PlateCopy.Wrap(existing)
                : PlateCopy.Make(game.Armor, spec, name);
            if (plate != null)
            {
                PlateTips.Set(gui, plate.Rect, spec.Topic, spec.Tip);
                ColumnLayout.Apply(gui.m_player, game);
            }
            return plate;
        }
    }
}
