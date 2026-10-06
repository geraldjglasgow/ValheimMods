using System.Collections.Generic;
using PackPanel.Look;
using PackPanel.Slots;
using UnityEngine;

namespace PackPanel.Panels
{
    /// <summary>
    /// Flat ochre item silhouettes for empty slots (<c>assets/icon_&lt;slot&gt;.png</c>, 64 px, the approved set in
    /// artwork/slot-icons-approved, drawn by artwork/slot-icons-silhouette-concept/draw.py), colour baked in.
    /// Slight transparency distinguishes the hint from an item occupying the slot.
    /// </summary>
    public static class SlotIcons
    {
        public static readonly Color Hint = new Color(1f, 1f, 1f, 0.85f);

        public static Color TintFor(SlotKind kind) => Hint;

        /// <summary>Each kind's icon, by kind, read once: the key ring's button and the Food and Mead bar ask every frame.</summary>
        private static readonly Dictionary<SlotKind, Sprite> icons = new Dictionary<SlotKind, Sprite>();

        public static Sprite For(SlotKind kind)
        {
            if (kind == SlotKind.Retired)
                return null;
            if (!icons.TryGetValue(kind, out Sprite icon) || (!ReferenceEquals(icon, null) && icon == null))
                icons[kind] = icon = SkinArt.Icon(kind.ToString().ToLowerInvariant());
            return icon;
        }

    }
}
