using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlateColumn
{
    /// <summary>
    /// The column as it stands in the scene, readied on every <see cref="Column.Arrange"/> and <see cref="Column.Add"/>;
    /// each step is safe to repeat and to run from any copy of this library. Readying finds the game's plates, finds or
    /// makes the container, adopts every member still sitting on the panel itself (the game's plates, plates an older
    /// copy made, OpenKeep's pre-library trash plate) by moving the very same objects into the container, gives the
    /// game's plates their tooltips, styles every member as a box, puts the members in rank order and lays the column out
    /// at once.
    /// </summary>
    internal sealed class BoxStack
    {
        private BoxStack(RectTransform boxes, GamePlates game)
        {
            Boxes = boxes;
            Game = game;
        }

        /// <summary>The container, <c>PlateColumn_boxes</c>.</summary>
        public RectTransform Boxes { get; }

        public GamePlates Game { get; }

        /// <summary>The readied column, or null (with nothing changed) when the game's plates are missing.</summary>
        public static BoxStack? Ready(InventoryGui gui)
        {
            GamePlates? game = GamePlates.Find(gui);
            if (game == null)
            {
                return null;
            }
            BoxStack stack = new BoxStack(BoxContainer.Get(gui.m_player), game);
            stack.Adopt(gui.m_player);
            game.Tip(gui);
            stack.Order();
            return stack;
        }

        /// <summary>
        /// Styles every member as a box, moves the members to the top of the container in rank order (anything else a
        /// mod put in the container goes after them), and lays the column out now rather than at the end of the frame,
        /// so positions read right away are true. Does nothing visible while the inventory is closed; the layout group
        /// lays out when it is shown.
        /// </summary>
        public void Order()
        {
            List<RectTransform> members = ColumnLayout.Sorted(Boxes, Game);
            for (int i = 0; i < members.Count; i++)
            {
                BoxStyle.Apply(members[i]);
                if (members[i].GetSiblingIndex() != i)
                {
                    members[i].SetSiblingIndex(i);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(Boxes);
        }

        /// <summary>Moves members that are direct children of the panel into the container, active or not.</summary>
        private void Adopt(RectTransform panel)
        {
            List<Transform> strays = new List<Transform>();
            foreach (Transform child in panel)
            {
                if (ColumnLayout.IsMember(child, Game))
                {
                    strays.Add(child);
                }
            }
            foreach (Transform stray in strays)
            {
                stray.SetParent(Boxes, false);
            }
        }
    }
}
