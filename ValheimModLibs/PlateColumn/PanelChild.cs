using UnityEngine;

namespace PlateColumn
{
    /// <summary>
    /// A direct child of the player panel looked up by name and kept, for <see cref="SeatWatch"/>, which runs every frame
    /// the inventory is open (a find by name walks the panel's children). Found again when the kept one was destroyed or
    /// left the panel, and on a frame the caller asks for it (the panel's children changed, or a while has passed: a mod
    /// may rename its box after adding it); a name not found stays unfound until then.
    /// </summary>
    internal sealed class PanelChild
    {
        private RectTransform? found;
        private bool looked;

        public PanelChild(string name) => Name = name;

        public string Name { get; }

        public RectTransform? In(RectTransform panel, bool recheck)
        {
            if (!recheck && looked && (found is null || (found != null && found.parent == panel)))
            {
                return found;
            }
            looked = true;
            found = panel.Find(Name) as RectTransform;
            return found;
        }
    }

    /// <summary>A seat in the container and the panel's box it holds the place of (<see cref="Seats"/>).</summary>
    internal sealed class SeatSlot
    {
        public SeatSlot(RectTransform seat, string occupant)
        {
            Seat = seat;
            Box = new PanelChild(occupant);
        }

        public RectTransform Seat { get; }

        public PanelChild Box { get; }
    }
}
