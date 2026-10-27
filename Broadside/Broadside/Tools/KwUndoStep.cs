using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One piece an undo step made. While it stands: its ZDO id and where it was made. Once undone: also its prefab, turn
    /// and saved ZDO values (<see cref="KwBpData"/>), so a redo builds it again as it was.
    /// </summary>
    public sealed class KwUndoPiece
    {
        public KwUndoPiece(ZDOID id, Vector3 position)
        {
            Id = id;
            Position = position;
        }

        public ZDOID Id { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; set; } = Quaternion.identity;
        public string Prefab { get; set; } = "";
        public List<KwBpValue> Data { get; set; } = new List<KwBpValue>();
    }

    /// <summary>
    /// One building action on the dock, undone as one step: everything one tool action built (a keel, a rib or an even row
    /// of ribs, a planking, deck or railing part or a whole keel-to-keel run of them, a ladder, a mast), one piece placed
    /// from the hammer (a hatch, a post cap, a rudder), one edit (<see cref="KwEditWork"/>: the parts it built again, each
    /// beside the part it replaced, <see cref="Replaced"/>), or one cabin removed with the hammer with what stood on it
    /// (<see cref="Removal"/>). It reads as its tool's name or the hammer piece's name; pieces
    /// of different kinds in one step (an edit carrying a cabin's stack) read as "parts"; an edit as "edit of" them, a removal
    /// as "removal of" them.
    /// </summary>
    public sealed class KwUndoStep
    {
        public KwUndoStep(string kind, bool tool, int frame)
        {
            Kind = kind;
            Tool = tool;
            Frame = frame;
        }

        /// <summary>The tool that built it ("keel", "rib", ...), the hammer piece's name, or "" for a mix.</summary>
        public string Kind { get; private set; }

        /// <summary>Whether <see cref="Kind"/> is a tool's name.</summary>
        public bool Tool { get; }

        /// <summary>The frame its parts were built in; -1 for a step built again by a redo, which nothing joins.</summary>
        public int Frame { get; }

        public List<KwUndoPiece> Pieces { get; } = new List<KwUndoPiece>();

        /// <summary>
        /// For an edit: the pieces it took away, each kept to build again in the place of the piece at the same index in
        /// <see cref="Pieces"/>. Undoing or redoing an edit swaps the two (<see cref="KwUndoWork.Swap"/>); empty for any other step.
        /// </summary>
        public List<KwUndoPiece> Replaced { get; } = new List<KwUndoPiece>();

        /// <summary>
        /// Whether the step is pieces the hammer removed together (a cabin and what stood on it, <see cref="KwCabinRemoval"/>):
        /// it swaps, undo building <see cref="Replaced"/> again and redo taking them again, even while one side is empty.
        /// </summary>
        public bool Removal { get; set; }

        /// <summary>Whether the step is an edit or a removal: undone and redone by swapping its pieces with the ones they replaced.</summary>
        public bool Swaps => Removal || Replaced.Count > 0;

        /// <summary>How many pieces it holds to undo or redo: its own, and for a removal the ones it took.</summary>
        public int Count => Pieces.Count + (Removal ? Replaced.Count : 0);

        /// <summary>One more part of the same action; a part of another kind makes the step a mix.</summary>
        public void Join(string kind, bool tool)
        {
            if (kind != Kind || tool != Tool)
                Kind = "";
        }

        /// <summary>
        /// The step as the player reads it, for this many of its pieces: "keel", "3 ribs", "5 planking parts", "Small hatch".
        /// </summary>
        public string Words(int count) => (Removal ? "removal of " : Swaps ? "edit of " : "") + Named(count);

        private string Named(int count)
        {
            if (Kind.Length == 0)
                return count == 1 ? "1 part" : $"{count} parts";
            if (!Tool)
                return count == 1 ? Kind : $"{count} pieces";
            string label = KwToolPieces.Label(Kind).ToLowerInvariant();
            if (count == 1)
                return label;
            return Kind == "keel" || Kind == "rib" || Kind == "ladder" ? $"{count} {label}s" : $"{count} {label} parts";
        }
    }
}
