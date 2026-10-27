using System;
using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// What a sea edit did to one entry of a launched ship's blueprint, the same on every peer: its index, the piece there
    /// before and after (null for none: an entry that was not there yet, or a tombstone, <see cref="KwSeaApply.Gone"/>).
    /// </summary>
    public readonly struct KwSeaChange
    {
        public KwSeaChange(int index, KwBpPiece? old, KwBpPiece? now) => (Index, Old, New) = (index, old, now);

        public int Index { get; }
        public KwBpPiece? Old { get; }
        public KwBpPiece? New { get; }

        public bool Added => Old == null && New != null;
        public bool Removed => Old != null && New == null;
        public bool Changed => Old != null && New != null;

        /// <summary>The piece as it is now, else as it was.</summary>
        public KwBpPiece? Either => New ?? Old;
    }

    /// <summary>
    /// The entries a sea edit (or several, for a peer that missed some) changed in a launched ship's blueprint, in index
    /// order, and the share of them a kind of part has (<see cref="Mine"/>). Pure.
    /// </summary>
    public sealed class KwSeaChanges
    {
        public KwSeaChanges(List<KwSeaChange> all) => All = all;

        public List<KwSeaChange> All { get; }

        public bool Empty => All.Count == 0;

        /// <summary>The changes whose piece, before or after, a kind owns.</summary>
        public KwSeaChanges Mine(KwSeaKind kind) => Where(p => kind.Owns(p));

        /// <summary>The changes whose piece, before or after, is one of these.</summary>
        public KwSeaChanges Where(Func<KwBpPiece, bool> which) =>
            new KwSeaChanges(All.FindAll(c => (c.Old != null && which(c.Old)) || (c.New != null && which(c.New))));

        /// <summary>Whether any change's piece, before or after, is one of these.</summary>
        public bool Any(Func<KwBpPiece, bool> which) => All.Exists(c => (c.Old != null && which(c.Old)) || (c.New != null && which(c.New)));

        /// <summary>The indices changed or removed (whose old marks go), and the ones added.</summary>
        public HashSet<int> Indices() => new HashSet<int>(All.ConvertAll(c => c.Index));

        public override string ToString() =>
            string.Join(", ", All.ConvertAll(c => (c.Added ? "+" : c.Removed ? "-" : "~") + c.Index + " " + (c.Either?.Prefab ?? "?")));
    }
}
