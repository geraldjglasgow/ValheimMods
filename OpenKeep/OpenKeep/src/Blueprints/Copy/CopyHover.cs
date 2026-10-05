using System.Collections.Generic;
using System.Linq;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// What a click would take now (the hover preview): the piece under the crosshair, with Shift its building,
    /// with G its joined pieces of the same type; and whether the click would let them go instead (every one of them is
    /// selected already). Set again only when the aimed piece, the mode, the building found or the selection changed;
    /// <see cref="Version"/> changes with it so the glow follows.
    /// </summary>
    public static class CopyHover
    {
        private static readonly List<FixPiece> None = new List<FixPiece>();
        private static (Piece Aimed, CopyMode Mode, List<FixPiece> Building, int Selection) stamp;

        public static List<FixPiece> Pieces { get; private set; } = None;

        /// <summary>A click would let the hovered pieces go (all of them are selected).</summary>
        public static bool Drops { get; private set; }

        /// <summary>How many of the hovered pieces are not selected yet (what a click adds).</summary>
        public static int Adds { get; private set; }

        public static CopyMode Mode { get; private set; }

        public static int Version { get; private set; }

        public static void Update()
        {
            Piece aimed = CopyAim.Piece;
            CopyMode mode = CopyKeys.Mode;
            List<FixPiece> building = aimed != null && mode != CopyMode.Piece ? CopyBuildings.Of(aimed, now: false) : null;
            var now = (aimed, mode, building, CopySelection.Version);
            if (ReferenceEquals(now.aimed, stamp.Aimed) && now.mode == stamp.Mode && ReferenceEquals(now.building, stamp.Building) && now.Version == stamp.Selection)
                return;
            stamp = now;
            Mode = mode;
            Set(Take(aimed, mode, building));
        }

        /// <summary>Nothing aimed at (the tool is hidden).</summary>
        public static void Reset()
        {
            stamp = default;
            if (Pieces.Count > 0)
                Set(None);
        }

        /// <summary>The pieces a click takes in the mode; empty when nothing is aimed at or its building is not known yet.</summary>
        public static List<FixPiece> Take(Piece aimed, CopyMode mode, List<FixPiece> building)
        {
            if (aimed == null)
                return None;
            if (mode == CopyMode.Piece)
                return new List<FixPiece> { GroundFixBuilding.Describe(aimed) };
            if (building == null)
                return None;
            return mode == CopyMode.SameType ? CopySameType.Of(building, aimed) : building;
        }

        /// <summary>The hover becomes these pieces; the glow is told only when they or what a click does to them changed.</summary>
        private static void Set(List<FixPiece> pieces)
        {
            int adds = pieces.Count(p => !CopySelection.Contains(p.Piece));
            if (ReferenceEquals(pieces, Pieces) && adds == Adds)
                return;
            Pieces = pieces;
            Adds = adds;
            Drops = pieces.Count > 0 && adds == 0;
            Version++;
        }
    }
}
