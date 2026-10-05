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

        /// <summary>Each kind's icon name, made once: the key ring's button asks every frame.</summary>
        private static readonly Dictionary<SlotKind, string> names = new Dictionary<SlotKind, string>();

        public static Sprite For(SlotKind kind)
        {
            if (kind == SlotKind.Retired)
                return null;
            if (!names.TryGetValue(kind, out string name))
                names[kind] = name = kind.ToString().ToLowerInvariant();
            return SkinArt.Icon(name);
        }

    }
}
