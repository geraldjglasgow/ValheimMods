using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A saved ship made longer (or shorter) amidships, for <see cref="KwBpReshape"/>. The stretch goes where the hull
    /// is straight: the longest stretch along the ship that only straight parts cover (planking, decks and railings that
    /// run or taper from rib to rib, the straight middle of a part laid from keel end to keel end, keels that do not
    /// climb), clear of every stem, flat stern and climbing keel. Along it every place is spread out evenly
    /// (<see cref="KwBpAlong"/>); everything forward of it moves forward by the length added, everything aft of it stays.
    /// So the straight parts grow by the length of the stretch they cover, the ribs, masts, hatches and fittings in it
    /// spread apart, and the bow and stern keep their shape. Where ribs end up half as far apart again as they stood,
    /// more of the same rib are put between them, as far apart as before; in a stretch made shorter, ribs squeezed under
    /// half a metre apart go, but for those a part ends at.
    /// </summary>
    internal static class KwBpStretch
    {
        private const float Least = 0.5f, ShortestShare = 0.25f, FillFrom = 1.5f, Closest = 0.5f;

        public static KwBlueprint? Apply(KwBlueprint blueprint, float share, out string? error)
        {
            KwBpHull hull = KwBpHull.Measure(blueprint);
            KwBpAlong? along = Along(blueprint, hull, share * hull.Length, out error);
            if (along == null)
                return null;
            KwBlueprint stretched = new KwBlueprint(blueprint.Name);
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                KwBpPiece? moved = Move(piece, along.Value, out error);
                if (moved == null)
                    return null;
                stretched.Pieces.Add(moved);
            }
            Fill(stretched, along.Value);
            Thin(stretched, along.Value);
            stretched.Pieces.Sort((a, b) => a.Position.z.CompareTo(b.Position.z));
            return stretched;
        }

        // The stretch the length is added to, spread over it; null and why when there is none, or too little to take it away.
        private static KwBpAlong? Along(KwBlueprint blueprint, KwBpHull hull, float added, out string? error)
        {
            (float From, float To)? zone = KwBpStretchZone.Find(blueprint, hull);
            float span = zone != null ? zone.Value.To - zone.Value.From : 0f, most = span * (1f - ShortestShare);
            error = span < Least ? "it has no straight middle to stretch: the hull is all stem and stern" : null;
            if (error == null && span + added < span * ShortestShare)
                error = $"its straight middle is {KwBpPartText.Num(span)} m long, so it can be at most {KwBpPartText.Num(most)} m"
                    + $" ({most / hull.Length * 100f:0}%) shorter";
            return error == null ? new KwBpAlong(zone!.Value.From, zone.Value.To, (span + added) / span) : (KwBpAlong?)null;
        }

        // A piece moved along: a straight part from its new start to its new end, any other piece to its new place.
        private static KwBpPiece? Move(KwBpPiece piece, KwBpAlong along, out string? error)
        {
            error = null;
            Vector3 at = piece.Position;
            Vector3 moved = new Vector3(at.x, at.y, along.Map(at.z));
            if (!KwBpStretchZone.Extent(piece, out float length, out float way))
                return KwBpPartText.Moved(piece, moved, null);
            float grown = Mathf.Abs(along.Map(at.z + way * length) - moved.z);
            if (Mathf.Abs(grown - length) < 0.001f)
                return KwBpPartText.Moved(piece, moved, null);
            string? text = KwBpPartText.Lengthened(KwBpPartText.Tool(piece), KwShipParts.Saved(piece), grown, out error);
            return text != null ? KwBpPartText.Moved(piece, moved, text) : null;
        }

        // More of the same rib between two in the stretch that now stand much further apart than they did.
        private static void Fill(KwBlueprint ship, KwBpAlong along)
        {
            if (along.Factor < FillFrom)
                return;
            List<KwBpPiece> ribs = ship.Pieces.FindAll(p => KwBpPartText.Tool(p) == "rib" && along.Within(p.Position.z));
            ribs.Sort((a, b) => a.Position.z.CompareTo(b.Position.z));
            int between = Mathf.RoundToInt(along.Factor) - 1;
            for (int i = 0; i + 1 < ribs.Count; i++)
            {
                KwBpPiece aft = ribs[i], fore = ribs[i + 1];
                if (KwShipParts.Saved(aft) != KwShipParts.Saved(fore) || Mathf.Abs(aft.Position.x - fore.Position.x) > 0.01f)
                    continue;
                for (int n = 1; n <= between; n++)
                    ship.Pieces.Add(KwBpPartText.Moved(aft, Vector3.Lerp(aft.Position, fore.Position, n / (between + 1f)), null));
            }
        }

        // A shortened stretch: ribs squeezed closer than Closest to the last one kept go, unless a part ends at them.
        private static void Thin(KwBlueprint ship, KwBpAlong along)
        {
            if (along.Factor >= 1f)
                return;
            List<float> ends = new List<float>();
            foreach (KwBpPiece piece in ship.Pieces)
            {
                if (KwBpStretchZone.Extent(piece, out float length, out float way))
                    ends.AddRange(new[] { piece.Position.z, piece.Position.z + way * length });
            }
            List<KwBpPiece> ribs = ship.Pieces.FindAll(p => KwBpPartText.Tool(p) == "rib" && along.Within(p.Position.z));
            ribs.Sort((a, b) => a.Position.z.CompareTo(b.Position.z));
            float kept = float.MinValue;
            foreach (KwBpPiece rib in ribs)
            {
                float z = rib.Position.z;
                if (z - kept >= Closest || ends.Exists(e => Mathf.Abs(e - z) < 0.01f))
                    kept = z;
                else
                    ship.Pieces.Remove(rib);
            }
        }
    }

    /// <summary>
    /// Places along the ship spread out over a stretch: aft of it as they are, in it from its aft end by a factor, forward
    /// of it moved forward by what the stretch grew.
    /// </summary>
    internal readonly struct KwBpAlong
    {
        public KwBpAlong(float from, float to, float factor)
        {
            From = from;
            To = to;
            Factor = factor;
        }

        public float From { get; }
        public float To { get; }
        public float Factor { get; }

        public float Map(float z) => z <= From ? z : z >= To ? z + (To - From) * (Factor - 1f) : From + (z - From) * Factor;

        /// <summary>Whether a place now stands in the stretch (its ends included).</summary>
        public bool Within(float z) => z >= From - 0.01f && z <= Map(To) + 0.01f;
    }
}
