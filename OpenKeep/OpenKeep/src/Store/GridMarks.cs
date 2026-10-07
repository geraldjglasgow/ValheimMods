using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Store
{
    /// <summary>
    /// What the favourite marks of one inventory grid were last drawn for: its inventory, its elements (the game makes
    /// them anew when the grid's size changes), the inventories' change count, the favourites' revision and the switches.
    /// <see cref="FavouriteOverlay"/> draws the marks again only when one of them changed, and at least every
    /// <see cref="MaxAge"/> seconds in case an inventory was changed without the game's change call. On the grid's own
    /// object, so it goes with the grid.
    /// </summary>
    public sealed class GridMarks : MonoBehaviour
    {
        private const float MaxAge = 0.25f;

        private Inventory inventory;
        private InventoryElement first;
        private int count = -1;
        private int change = -1;
        private int revision = -1;
        private bool show;
        private bool playerGrid;
        private float at = float.MinValue;

        public static GridMarks Of(InventoryGrid grid)
        {
            GridMarks marks = grid.GetComponent<GridMarks>();
            return marks != null ? marks : grid.gameObject.AddComponent<GridMarks>();
        }

        /// <summary>True when the marks drawn last still hold; otherwise remembers what they are drawn for now.</summary>
        public bool Current(Inventory now, List<InventoryElement> elements, bool showing, bool forPlayer)
        {
            InventoryElement head = elements.Count > 0 ? elements[0] : null;
            float time = Time.unscaledTime;
            if (now == inventory && head == first && elements.Count == count && InventoryChanges.Count == change
                && Favourites.Revision == revision && showing == show && forPlayer == playerGrid && time - at < MaxAge)
                return true;
            inventory = now;
            first = head;
            count = elements.Count;
            change = InventoryChanges.Count;
            revision = Favourites.Revision;
            show = showing;
            playerGrid = forPlayer;
            at = time;
            return false;
        }
    }
}
