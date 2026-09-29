using UnityEngine;

namespace PlateColumn
{
    /// <summary>
    /// The game's two readouts, armour and weight: each an object holding a wood background, a small icon and the text
    /// the game rewrites every frame (<c>InventoryGui.m_armor</c>, <c>m_weight</c>). The scene has them as direct
    /// children of the player panel (<c>InventoryGui.m_player</c>) until the column adopts them into its container, so
    /// each is found by walking up from its text until the parent is the panel or the container.
    /// <para>
    /// This lookup is also how older copies of the library stand down. An older copy walks up only until the parent is
    /// the panel; once both plates are in the container it lands on the container for both, sees one object where it
    /// expects two, and treats the game's plates as missing - its <c>Arrange</c> and <c>Add</c> then change nothing. So
    /// the first current copy to arrange takes the column over and no older copy moves or restyles anything after it.
    /// </para>
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

        /// <summary>Both plates, or null when either is missing or both texts lead to the same object.</summary>
        public static GamePlates? Find(InventoryGui gui)
        {
            if (gui == null || gui.m_player == null || gui.m_armor == null || gui.m_weight == null)
            {
                return null;
            }
            RectTransform? armor = PlateOf(gui.m_player, gui.m_armor.transform);
            RectTransform? weight = PlateOf(gui.m_player, gui.m_weight.transform);
            if (armor == null || weight == null || armor == weight)
            {
                return null;
            }
            return new GamePlates(armor, weight);
        }

        /// <summary>
        /// Gives each plate a tooltip unless a copy of this library already did (an older copy's tip keeps working: it
        /// pins its box beside whatever object it sits on).
        /// </summary>
        public void Tip(InventoryGui gui)
        {
            PlateTips.SetIfMissing(gui, Armor, ArmorTopic, ArmorTip);
            PlateTips.SetIfMissing(gui, Weight, WeightTopic, WeightTip);
        }

        /// <summary>The object holding <paramref name="text"/> whose parent is the panel or the column's container.</summary>
        private static RectTransform? PlateOf(RectTransform panel, Transform text)
        {
            Transform? t = text;
            while (t != null && t.parent != panel && !BoxContainer.Is(t.parent, panel))
            {
                t = t.parent;
            }
            return t as RectTransform;
        }
    }
}
