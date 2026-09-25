using System;

namespace EliteCrafting.Affixes
{
    /// <summary>
    /// The raw, parsed content of an item's <c>ecf_</c> keys, before any definition is looked up. Shared by every
    /// <see cref="ItemState"/> resolved from it (a rules reload re-resolves without re-parsing). Immutable.
    /// </summary>
    internal sealed class StateData
    {
        public static readonly StateData Empty = new StateData();

        public int Format { get; set; } = ItemKeys.CurrentFormat;

        /// <summary>Written by a newer version of the mod: read what parses, never write back.</summary>
        public bool Newer { get; set; }

        public string? RarityId { get; set; }
        public ItemSegment[] Segments { get; set; } = Array.Empty<ItemSegment>();
        public string? BoundId { get; set; }
        public float Refine { get; set; }

        /// <summary>An <c>ecf_refine</c> value that did not parse, kept verbatim and written back (never destroyed).</summary>
        public string? RefineRaw { get; set; }
        public string? SealedReason { get; set; }
        public string? SigilId { get; set; }

        /// <summary>The reserved <c>ecf_tier</c> value, preserved verbatim.</summary>
        public string? ReservedTier { get; set; }

        public bool IsEmpty =>
            RarityId == null && Segments.Length == 0 && BoundId == null && Refine == 0f && RefineRaw == null && SealedReason == null
            && SigilId == null && ReservedTier == null && !Newer;

        public StateData Copy() => (StateData)MemberwiseClone();
    }
}
