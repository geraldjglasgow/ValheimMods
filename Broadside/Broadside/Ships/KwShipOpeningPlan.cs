using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>One opening of a launched ship as its plan has it (<see cref="KwShipOpeningPlan"/>).</summary>
    public sealed class KwShipOpening
    {
        public KwShipOpening(int piece, KwOpening opening, int number, float lowest, bool floods, string note)
        {
            (Piece, Opening, Number, Lowest, Floods, Note) = (piece, opening, number, lowest, floods, note);
        }

        /// <summary>Its part's index among the blueprint's pieces.</summary>
        public int Piece { get; }
        public KwOpening Opening { get; }

        /// <summary>Its number among the ship's covered openings, which its state is kept under; -1 for one with no cover, always open.</summary>
        public int Number { get; }

        /// <summary>Its lowest point's height in the ship's frame.</summary>
        public float Lowest { get; }

        /// <summary>Whether it is in the hull's side (its planking, or a cabin's raised side), so the sea through it fills the hold.</summary>
        public bool Floods { get; }

        /// <summary>The note under its leaves' hover ("" for none).</summary>
        public string Note { get; }
    }

    /// <summary>
    /// A launched ship's openings, planned from its blueprint the same on every peer (pure arithmetic in the ship's frame,
    /// <see cref="KwShipOpenings"/> makes them in the world): every planking part's and cabin's openings that fit their walls
    /// (<see cref="KwOpeningSurfaces"/>), in the blueprint's piece order and then each part's walls' and openings' order;
    /// every covered one numbered from 0 in that order (all its leaves share its number, a double door's two as a pair of
    /// shutters' two, and its state is kept under it);
    /// its leaves (<see cref="KwOpeningLeaves"/>); and the note "Below the waterline" for one in the hull's sides whose
    /// lowest point is under the design waterline and <see cref="Margin"/>. The flooding's holes: the bands
    /// (<see cref="KwOpeningBands"/>) of the openings in the hull's sides whose foot is under the waterline and
    /// <see cref="Reach"/>, each a <see cref="KwHullHole"/> of the band's area at its middle on the outer face (part -1: no
    /// part's hole to mend), lowest first. A cabin's own walls stand on a deck, so water through them would not reach the
    /// hold: they never flood.
    /// </summary>
    public sealed class KwShipOpeningPlan
    {
        /// <summary>The hover note of an opening low in the hull's side.</summary>
        public const string Below = "Below the waterline: shut it at sea";

        /// <summary>How far over the waterline an opening's foot still gets the note, and a band still lets the sea in.</summary>
        public const float Margin = 0.2f, Reach = 1f;

        /// <summary>The most holes the open openings let the sea in through at once (the lowest ones).</summary>
        public const int Most = 48;

        private readonly float waterline;
        private readonly List<(KwHullHole Hole, int Number)> bands = new List<(KwHullHole Hole, int Number)>();

        // The blueprint's keels, which its planking is seated on as its look is (KwSeat), so the leaves hang in its cuts.
        private readonly KwKeelLines keels;

        private KwShipOpeningPlan(float waterline, KwKeelLines keels) => (this.waterline, this.keels) = (waterline, keels);

        public List<KwShipOpening> Openings { get; } = new List<KwShipOpening>();

        /// <summary>Every leaf: its part's index, its plan, its opening's number and the note under its hover.</summary>
        public List<(int Piece, KwLeafPlan Leaf, int Number, string Note)> Leaves { get; } = new List<(int Piece, KwLeafPlan Leaf, int Number, string Note)>();

        /// <summary>How many covered openings (numbered 0 to this less one).</summary>
        public int Covered { get; private set; }

        /// <summary>Every band that may let the sea in, lowest first, with its opening's number (-1: no cover).</summary>
        public IReadOnlyList<(KwHullHole Hole, int Number)> Bands => bands;

        /// <summary>
        /// A ship's openings from its blueprint in its own frame and its design waterline (the ship's frame's height); each
        /// part's openings fitted once when a cache is given (by its piece: an edit at sea replaces a changed part's piece, so
        /// only it is fitted again).
        /// </summary>
        public static KwShipOpeningPlan Of(KwBlueprint blueprint, float waterline, KwShipOpeningCache? cache = null)
        {
            KwShipOpeningPlan plan = new KwShipOpeningPlan(waterline, KwKeelLines.Of(blueprint));
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwBpPiece piece = blueprint.Pieces[i];
                foreach (KwShipOpeningFit fit in cache != null ? cache.Of(piece, plan.keels, waterline) : KwShipOpeningFit.Of(piece, plan.keels, waterline))
                    plan.Opening(i, fit);
            }
            plan.bands.Sort((a, c) => a.Hole.Place.y.CompareTo(c.Hole.Place.y));
            return plan;
        }

        /// <summary>
        /// The number each covered opening of one plan has in another plan of the same ship (an edit at sea in between), by its
        /// part's index and its own number on its wall; a covered opening gone, or no longer covered, is not in it. Pure.
        /// </summary>
        public static Dictionary<int, int> Renumber(KwShipOpeningPlan was, KwShipOpeningPlan now)
        {
            Dictionary<(int, int, string), int> numbers = new Dictionary<(int, int, string), int>();
            foreach (KwShipOpening o in now.Openings)
            {
                if (o.Number >= 0)
                    numbers[(o.Piece, o.Opening.Id, o.Opening.Wall)] = o.Number;
            }
            Dictionary<int, int> map = new Dictionary<int, int>();
            foreach (KwShipOpening o in was.Openings)
            {
                if (o.Number >= 0 && numbers.TryGetValue((o.Piece, o.Opening.Id, o.Opening.Wall), out int n))
                    map[o.Number] = n;
            }
            return map;
        }

        /// <summary>
        /// The covered openings' states as numbered in another plan of the same ship, from their states in this one: each
        /// kept with its opening, a new one shut (0). Pure.
        /// </summary>
        public static int[] States(KwShipOpeningPlan was, int[] states, KwShipOpeningPlan now)
        {
            int[] moved = new int[now.Covered];
            foreach (KeyValuePair<int, int> pair in Renumber(was, now))
            {
                if (pair.Key < states.Length && pair.Value < moved.Length)
                    moved[pair.Value] = states[pair.Key];
            }
            return moved;
        }

        /// <summary>
        /// The holes the sea comes in through for the openings' states (each covered one's by its number: 0 shut), the lowest
        /// first and at most <see cref="Most"/>, into a list cleared first (nothing made).
        /// </summary>
        public void Leaks(int[] states, List<KwHullHole> into)
        {
            into.Clear();
            for (int i = 0; i < bands.Count && into.Count < Most; i++)
            {
                int n = bands[i].Number;
                if (n < 0 || (n < states.Length && states[n] != 0))
                    into.Add(bands[i].Hole);
            }
        }

        // One opening: numbered when covered, its leaves, its note, its bands when it lets the sea in low enough.
        private void Opening(int index, KwShipOpeningFit fit)
        {
            KwOpening o = fit.Opening;
            int number = o.Covered ? Covered++ : -1;
            string note = fit.Floods && fit.Lowest < waterline + Margin ? Below : "";
            Openings.Add(new KwShipOpening(index, o, number, fit.Lowest, fit.Floods, note));
            foreach (KwLeafPlan leaf in fit.Leaves)
                Leaves.Add((index, leaf, number, note));
            foreach (KwHullHole band in fit.Bands)
                bands.Add((band, number));
        }

        /// <summary>A kit point of a piece in the ship's frame.</summary>
        public static Vector3 Ship(KwBpPiece piece, Vector3 kit) => piece.Position + piece.Rotation * KwMesh.ToUnity(kit);
    }

    /// <summary>
    /// One opening of a launched ship's part fitted to its wall, as every plan of the ship numbers it
    /// (<see cref="KwShipOpeningPlan"/>): the opening, its lowest point in the ship's frame, whether it is in the hull's
    /// side, its leaves, and the bands low enough to let the sea in, as holes on the wall's outer face. Pure.
    /// </summary>
    public sealed class KwShipOpeningFit
    {
        private KwShipOpeningFit(KwOpening opening, float lowest, bool floods) => (Opening, Lowest, Floods) = (opening, lowest, floods);

        public KwOpening Opening { get; }
        public float Lowest { get; }
        public bool Floods { get; }
        public List<KwLeafPlan> Leaves { get; } = new List<KwLeafPlan>();
        public List<KwHullHole> Bands { get; } = new List<KwHullHole>();

        /// <summary>A piece's openings fitted (none for a piece that is not planking or a cabin).</summary>
        public static List<KwShipOpeningFit> Of(KwBpPiece piece, KwKeelLines keels, float waterline)
        {
            List<KwShipOpeningFit> fits = new List<KwShipOpeningFit>();
            if (!piece.Prefab.StartsWith(KwToolPieces.PartPrefix))
                return fits;
            string tool = piece.Prefab.Substring(KwToolPieces.PartPrefix.Length);
            if (tool != "planking" && !KwHouses.Owns(tool))
                return fits;
            foreach (KwOpeningWall wall in KwOpeningSurfaces.Of(tool, KwShipParts.Saved(piece), new KwSeatAt(keels, piece.Position, piece.Rotation * Vector3.forward)))
            {
                foreach (KwOpening o in wall.Openings)
                    fits.Add(Fitted(piece, wall.Surface, o, waterline));
            }
            return fits;
        }

        private static KwShipOpeningFit Fitted(KwBpPiece piece, KwOpeningSurface surface, KwOpening o, float waterline)
        {
            KwShipOpeningFit fit = new KwShipOpeningFit(o, LowestOf(piece, surface, o), surface is KwPlankSurface);
            fit.Leaves.AddRange(KwOpeningLeaves.For(surface, o));
            if (fit.Floods && fit.Lowest < waterline + KwShipOpeningPlan.Reach)
                fit.Band(piece, surface, waterline);
            return fit;
        }

        // The opening's bands whose foot is low enough, as holes on its wall's outer face in the ship's frame.
        private void Band(KwBpPiece piece, KwOpeningSurface surface, float waterline)
        {
            foreach ((Vector2 middle, float area, float foot) in KwOpeningBands.Of(Opening))
            {
                if (KwShipOpeningPlan.Ship(piece, surface.Point(middle.x, foot, 0f)).y >= waterline + KwShipOpeningPlan.Reach)
                    continue;
                Vector3 place = KwShipOpeningPlan.Ship(piece, surface.Point(middle.x, middle.y, 0f));
                Vector3 normal = piece.Rotation * KwMesh.ToUnity(surface.Out(middle.x, middle.y));
                Bands.Add(new KwHullHole(-1, place, normal, Mathf.Sqrt(area / Mathf.PI)));
            }
        }

        // The lowest point of an opening's outline in the ship's frame.
        private static float LowestOf(KwBpPiece piece, KwOpeningSurface surface, KwOpening o)
        {
            float lowest = float.MaxValue;
            foreach (Vector2 p in o.Outline())
                lowest = Mathf.Min(lowest, KwShipOpeningPlan.Ship(piece, surface.Point(p.x, p.y, 0f)).y);
            return lowest;
        }
    }

    /// <summary>
    /// A launched ship's parts' openings fitted once each (<see cref="KwShipOpeningFit"/>), by their blueprint piece: an edit
    /// at sea gives a changed part a new piece and leaves the others' (<see cref="KwSeaApply"/>), so planning the ship again
    /// fits only the parts changed. Kept by the ship's openings (<see cref="KwShipOpenings"/>); the pieces not asked for
    /// since the last <see cref="Keep"/> are let go there.
    /// </summary>
    public sealed class KwShipOpeningCache
    {
        private Dictionary<KwBpPiece, List<KwShipOpeningFit>> fits = new Dictionary<KwBpPiece, List<KwShipOpeningFit>>();
        private Dictionary<KwBpPiece, List<KwShipOpeningFit>> used = new Dictionary<KwBpPiece, List<KwShipOpeningFit>>();
        private float waterline = float.NaN;

        /// <summary>A piece's openings fitted, from the cache when it was fitted before at this waterline.</summary>
        public List<KwShipOpeningFit> Of(KwBpPiece piece, KwKeelLines keels, float line)
        {
            if (line != waterline)
                (fits, waterline) = (new Dictionary<KwBpPiece, List<KwShipOpeningFit>>(), line);
            if (!fits.TryGetValue(piece, out List<KwShipOpeningFit> found))
                fits[piece] = found = KwShipOpeningFit.Of(piece, keels, line);
            used[piece] = found;
            return found;
        }

        /// <summary>Only the pieces asked for since the last call kept (the ship as it was planned last).</summary>
        public void Keep() => (fits, used) = (used, new Dictionary<KwBpPiece, List<KwShipOpeningFit>>());
    }
}
