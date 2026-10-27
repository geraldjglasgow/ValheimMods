using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the hammer's remove takes of a launched ship's railing (<see cref="KwSeaRails"/>), plain arithmetic on its text:
    /// a hull railing on both sides loses the side whose stretch was aimed at (its run: the first side drawn is run 0,
    /// <see cref="KwRailing.Drawn"/>) and keeps the other with its spacing; a roof railing loses the railed edge nearest
    /// the aim; the last side or edge removes the part. Its words say which.
    /// </summary>
    public static class KwSeaRailCut
    {
        public static KwSeaEdit? Removal(KwSeaShip ship, KwSeaPick pick)
        {
            if (pick.Index < 0 || pick.Index >= ship.Blueprint.Pieces.Count)
                return null;
            (KwBpPiece? left, string done) = Cut(ship.Blueprint.Pieces[pick.Index], pick.Part / 1000, pick.Point);
            KwSeaPart part = left != null ? KwSeaPart.Change(pick.Index, left) : KwSeaPart.Remove(pick.Index);
            return new KwSeaEdit("rail", ship.Revision, done, new List<KwSeaPart> { part });
        }

        /// <summary>The railing left when a run of it (or the roof edge nearest a point, the ship's frame) is taken; null when nothing is left.</summary>
        public static (KwBpPiece? Left, string Done) Cut(KwBpPiece piece, int run, Vector3 point)
        {
            string saved = KwSeaPiece.Text(piece);
            if (KwRoofRail.TryParse(saved, out KwRoofRail roof))
            {
                char side = Edge(roof, piece, point);
                string on = roof.On.Replace(side.ToString(), "");
                return on.Length == 0 || side == '?' ? (null, "Roof railing removed") : (KwSeaPiece.With(piece, roof.With(roof.Spacing, on).ToString(), null), "Roof railing's edge removed");
            }
            if (!KwSpan.TryParse(saved, out KwSpan span))
                return (null, "Railing removed");
            List<float> sides = KwRailing.Drawn(KwRailText.Sides(span.Rail));
            if (sides.Count < 2 || run < 0 || run > 1)
                return (null, "Railing removed");
            string keep = KwSeaRailSite.Letter(sides[1 - run]);
            return (KwSeaPiece.With(piece, KwRailText.With(saved, KwRailText.Spacing(span.Rail), keep), null), "Railing removed on that side");
        }

        // The railed edge of a roof railing nearest a point of the ship's frame: its side's letter ('?' for none).
        private static char Edge(KwRoofRail roof, KwBpPiece piece, Vector3 point)
        {
            Vector3 kit = KwMesh.ToKit(KwSeaApply.Back(piece.Rotation) * (point - piece.Position));
            Vector2 at = new Vector2(kit.x, kit.y);
            char best = '?';
            float near = float.MaxValue;
            foreach (KwRoofSegment edge in roof.Railed)
            {
                float d = (edge.Nearest(at) - at).sqrMagnitude;
                if (d < near)
                    (near, best) = (d, edge.Side);
            }
            return best;
        }
    }
}
