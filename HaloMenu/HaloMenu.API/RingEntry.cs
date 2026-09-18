using System;
using UnityEngine;

namespace HaloMenu.API
{
    /// <summary>
    /// One selectable option in a ring. Register it with <see cref="HaloMenuAPI.Register"/> for the default ring,
    /// or <see cref="Ring.Add"/> for a ring your mod owns.
    /// </summary>
    public sealed class RingEntry
    {
        /// <summary>Namespaced and unique ("yourmod.your_entry"). Registering a duplicate Id replaces the prior
        /// entry and logs a warning.</summary>
        public string Id;

        /// <summary>Shown in the ring's center label when this entry is hovered. Not drawn on the segment itself.</summary>
        public string Label;

        /// <summary>Letterboxed into its segment's icon square, aspect preserved. HaloMenu never loads, copies or
        /// unloads this reference; keep it alive for as long as the entry is registered.</summary>
        public Sprite Icon;

        /// <summary>Sorts entries clockwise from the ring's start angle. Entries beyond the ring's segment count
        /// are dropped, not wrapped to a second ring.</summary>
        public int Order;

        /// <summary>False skips this entry entirely; it does not consume a segment. Evaluated once when the ring
        /// opens. Defaults to always visible.</summary>
        public Func<bool> IsVisible = () => true;

        /// <summary>False keeps the segment (still highlightable) but selecting it shakes and refuses instead of
        /// firing OnSelect. Evaluated every frame the ring is open. Defaults to always enabled.</summary>
        public Func<bool> IsEnabled = () => true;

        /// <summary>Fired once, after the ring has finished closing.</summary>
        public Action OnSelect;
    }
}
