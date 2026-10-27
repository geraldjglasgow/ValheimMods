using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Broadside
{
    /// <summary>A planking part's top edge on one side, as the railing tool aims at it on a launched ship: the part, the side, how far.</summary>
    public readonly struct KwSeaSheer
    {
        public KwSeaSheer(int index, float side, float distance, Vector3 point) => (Index, Side, Distance, Point) = (index, side, distance, point);

        /// <summary>The planking part's index in the blueprint.</summary>
        public int Index { get; }

        /// <summary>+1 its own left (kit +y), -1 its right.</summary>
        public float Side { get; }
        public float Distance { get; }

        /// <summary>The nearest point on the edge (the ship's frame).</summary>
        public Vector3 Point { get; }
    }

    /// <summary>
    /// Where the railing tool's click on a launched ship lands, from its blueprint alone (plain arithmetic): the planking
    /// part whose top edge is nearest the aim on either side (<see cref="Sheer"/>; the edges worked out once a ship, as its
    /// planking never changes at sea), the hull railing that stands over a planking part (<see cref="Over"/>: laid from the
    /// same clicks, or cut from such a railing where a cabin stands, as <see cref="KwBpCost"/> finds it), and the text of
    /// a new railing over it (<see cref="Text"/>: the planking's shape, frames, length and ends, as the railing tool lays it
    /// between the same ribs). Port and starboard: a launched ship's bow is its frame's +z.
    /// </summary>
    public static class KwSeaRailSite
    {
        private static readonly ConditionalWeakTable<KwBlueprint, List<(int Index, float Side, Vector3[] Edge)>> edges =
            new ConditionalWeakTable<KwBlueprint, List<(int Index, float Side, Vector3[] Edge)>>();

        private const string Planking = KwToolPieces.PartPrefix + "planking";

        /// <summary>The planking top edge nearest a point (the ship's frame) within reach; null for none.</summary>
        public static KwSeaSheer? Sheer(KwBlueprint blueprint, KwKeelLines keels, Vector3 point, float reach)
        {
            KwSeaSheer? best = null;
            foreach ((int index, float side, Vector3[] edge) in Edges(blueprint, keels))
            {
                Vector3 on = Nearest(edge, point);
                float d = (on - point).magnitude;
                if (d <= reach && (best == null || d < best.Value.Distance))
                    best = new KwSeaSheer(index, side, d, on);
            }
            return best;
        }

        /// <summary>Every planking part's top edges, each side's a line of points in the ship's frame (worked out once a blueprint).</summary>
        public static List<(int Index, float Side, Vector3[] Edge)> Edges(KwBlueprint blueprint, KwKeelLines keels)
        {
            if (edges.TryGetValue(blueprint, out List<(int, float, Vector3[])> made))
                return made;
            made = new List<(int, float, Vector3[])>();
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwBpPiece piece = blueprint.Pieces[i];
                if (piece.Prefab != Planking || !KwSpan.TryParse(KwSeaPiece.Text(piece), out KwSpan span))
                    continue;
                List<Station> st = new KwSeatAt(keels, piece.Position, piece.Rotation * Vector3.forward).Seated(span).Stations();
                foreach (float side in new[] { 1f, -1f })
                    made.Add((i, side, st.ConvertAll(s => KwSeaApply.OnShip(piece, Top(s, side))).ToArray()));
            }
            edges.Add(blueprint, made);
            return made;
        }

        // A station's top edge on one side, in the part's kit.
        private static Vector3 Top(Station s, float side)
        {
            Vector2 e = s.Half[s.Half.Length - 1];
            return new Vector3(s.X, side * e.x, e.y);
        }

        private static Vector3 Nearest(Vector3[] line, Vector3 point)
        {
            Vector3 best = line[0];
            for (int i = 0; i + 1 < line.Length; i++)
            {
                Vector3 run = line[i + 1] - line[i];
                float t = run.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(point - line[i], run) / run.sqrMagnitude) : 0f;
                Vector3 on = line[i] + run * t;
                if ((on - point).sqrMagnitude < (best - point).sqrMagnitude)
                    best = on;
            }
            return best;
        }

        /// <summary>The hull railing standing over a planking part (laid with it, or cut from one along it); -1 for none.</summary>
        public static int Over(KwBlueprint blueprint, int planking)
        {
            KwBpPiece plank = blueprint.Pieces[planking];
            string plankText = KwSeaPiece.Text(plank), line = Line(plankText);
            Vector3 along = plank.Rotation * Vector3.forward;
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwBpPiece rail = blueprint.Pieces[i];
                if (!KwRailStretches.IsRail(rail.Prefab))
                    continue;
                string text = KwSeaPiece.Text(rail);
                Vector3 way = rail.Rotation * Vector3.forward;
                bool laid = (rail.Position - plank.Position).sqrMagnitude < 0.0025f && Vector3.Dot(way, along) > 0.999f && Line(text) == line;
                if (laid || KwRailCut.Covers(rail.Position, way, text, plank.Position, along, plankText))
                    return i;
            }
            return -1;
        }

        /// <summary>A new hull railing's text over a planking part: its line, posts that far apart (0: at its stations), on these sides.</summary>
        public static string? Text(string plankText, float spacing, string sides)
        {
            if (!KwSpan.TryParse(plankText, out KwSpan span))
                return null;
            KwSpan rail = new KwSpan(span.Family, span.FrameA, span.FrameB, span.Length, span.End) { Rail = KwRailText.Write(spacing, sides) };
            return rail.ToString();
        }

        // A hull part's line in its saved text: its shape, frames, length and ends.
        private static string Line(string saved) => string.Join("|", System.Linq.Enumerable.Take(saved.Split('|'), 5));

        /// <summary>Which way a part runs on a launched ship: +1 to the bow (its frame's +z), -1 back.</summary>
        public static float Runs(KwBpPiece piece) => (piece.Rotation * Vector3.forward).z >= 0f ? 1f : -1f;

        /// <summary>A side (+1 left, -1 right) as the letter a railing's text keeps.</summary>
        public static string Letter(float side) => side > 0f ? KwRailText.Left : KwRailText.Right;
    }
}
