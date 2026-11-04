using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where other dock sections meet a section's four deck edges (left, right, start, end, in
    /// <see cref="KwPierDeck.Edges"/>' order): an edge of another section on the same line at the same deck height, facing
    /// it (a branch's start on this one's side, a section carrying on from this one's end, a dock run up to its side, two
    /// docks side by side). Each is kept as a span in metres along the edge from its first end (a side's from the
    /// section's start, an end's from its left), so the curb is left out there (<see cref="Runs"/>) and the two decks run
    /// into each other with no lip. A dock ladder hanging from an edge (<see cref="KwPierLadder"/>) opens it the same way
    /// across <see cref="LadderSpan"/> either side of it (<see cref="Ladders"/>; not a join: the edge stays open and over
    /// the water there), so a player climbing up steps off flush; so does a mooring post standing over a side's piling
    /// (<see cref="KwDockMooring"/>, <see cref="PostSpan"/>), so the curb ends against it. Worked out on every peer from the sections, ladders and posts
    /// loaded there (each one's saved shape and where it stands), when a section builds and again when one meeting it, or
    /// a ladder on it, comes or goes (<see cref="Touch"/>, <see cref="TouchLadder"/>); never saved or sent, so docks built
    /// before it and late joiners build the same. Plain maths.
    /// </summary>
    public sealed class KwPierGaps
    {
        public const int Left = 0, Right = 1, Start = 2, End = 3;

        /// <summary>How far either side of a dock ladder its edge is open (its curb left out).</summary>
        public const float LadderSpan = 0.35f;

        /// <summary>How far either side of a mooring post over a piling its side is open (the curb ends against the post).</summary>
        public const float PostSpan = 0.24f;

        /// <summary>How far in from a side edge a mooring post's axis may stand and still open it (it stands over a piling).</summary>
        private const float PostInside = 0.45f;

        private const float Near = 0.1f, Level = 0.05f, Facing = -0.99f, Least = 0.05f, ShortestRun = 0.15f;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private readonly List<Vector2>[] spans = { new List<Vector2>(), new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };
        private readonly List<Vector2>[] ladders = { new List<Vector2>(), new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };
        private readonly List<Vector2>[] openings = { new List<Vector2>(), new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };

        /// <summary>No other section meets it anywhere.</summary>
        public static readonly KwPierGaps None = new KwPierGaps();

        /// <summary>The spans of an edge another section meets (from, to: metres from its first end), in order, merged.</summary>
        public IReadOnlyList<Vector2> On(int edge) => spans[edge];

        /// <summary>
        /// The spans of an edge a dock ladder hangs from (<see cref="LadderSpan"/> either side of each) or a mooring post stands
        /// on over a piling (<see cref="PostSpan"/>), in order, merged.
        /// </summary>
        public IReadOnlyList<Vector2> Ladders(int edge) => ladders[edge];

        /// <summary>Every span of an edge left open (another section, a ladder), in order, merged: no curb there, and the rough wood's boards end flush.</summary>
        public IReadOnlyList<Vector2> Openings(int edge) => openings[edge];

        /// <summary>The sides' spans to the centimetre (the ends' never change the timbers), to know whether the curbs change.</summary>
        public string ShapeKey { get; private set; } = "";

        /// <summary>A section's gaps from every other built section loaded here.</summary>
        public static KwPierGaps Of(KwPier pier)
        {
            KwPierGaps gaps = new KwPierGaps();
            Line[]? mine = Lines(pier);
            if (mine == null)
                return gaps;
            foreach (KwPier other in KwPier.Loaded)
            {
                Line[]? theirs = other != pier ? Lines(other) : null;
                if (theirs != null)
                    gaps.Add(mine, theirs);
            }
            foreach (KwPierLadder ladder in KwPierLadder.Loaded)
            {
                if (ladder != null && ladder.Spec != null && ladder.Spec.OnDock)
                    gaps.AddOpening(mine, ladder.transform.position, Flat(ladder.transform.forward).normalized, 0f, LadderSpan);
            }
            foreach (KwDockMooring post in KwDockMooring.Loaded)
            {
                if (post != null)
                    gaps.AddOpening(mine, post.transform.position, post.Outward, PostInside, PostSpan);
            }
            gaps.Close();
            return gaps;
        }

        /// <summary>
        /// A dock ladder or a mooring post built or gone at a point: every built section whose deck it hangs from or stands on
        /// looks at its gaps again.
        /// </summary>
        public static void TouchLadder(Vector3 at)
        {
            foreach (KwPier pier in KwPier.Loaded)
            {
                KwPierSpec? spec = pier != null ? pier.Spec : null;
                if (spec == null)
                    continue;
                Vector3 local = pier!.transform.InverseTransformPoint(at);
                if (Mathf.Abs(local.x) <= spec.Width * 0.5f + Near && local.z >= -Near && local.z <= spec.Length + Near && Mathf.Abs(local.y) <= Level)
                    pier.Regap();
            }
        }

        /// <summary>Every other built section loaded here that meets this one looks at its own gaps again.</summary>
        public static void Touch(KwPier pier)
        {
            Line[]? mine = Lines(pier);
            if (mine == null)
                return;
            foreach (KwPier other in KwPier.Loaded)
            {
                Line[]? theirs = other != pier ? Lines(other) : null;
                if (theirs != null && Meet(mine, theirs))
                    other.Regap();
            }
        }

        /// <summary>The runs of an edge this long that no other section meets (where its curb stands), each long enough to show.</summary>
        public List<Vector2> Runs(int edge, float length)
        {
            List<Vector2> runs = new List<Vector2>();
            float at = 0f;
            foreach (Vector2 gap in openings[edge])
            {
                if (gap.x - at >= ShortestRun)
                    runs.Add(new Vector2(at, gap.x));
                at = Mathf.Max(at, gap.y);
            }
            if (length - at >= ShortestRun)
                runs.Add(new Vector2(at, length));
            return runs;
        }

        private void Add(Line[] mine, Line[] theirs)
        {
            for (int i = 0; i < mine.Length; i++)
            {
                foreach (Line other in theirs)
                {
                    Vector2? span = Overlap(mine[i], other);
                    if (span != null)
                        spans[i].Add(span.Value);
                }
            }
        }

        // A ladder standing on one of the edges, facing out across it at the deck's height (inside 0), or a mooring post on a
        // side standing over its pilings, its back out across it (inside: how far in from the edge its axis may stand): that
        // edge open round it, span either side. Kept with the ladders (not a join: the edge stays open there).
        private void AddOpening(Line[] mine, Vector3 at, Vector3 outward, float inside, float span)
        {
            for (int i = 0; i < mine.Length; i++)
            {
                Line edge = mine[i];
                Vector3 along = Flat(edge.B - edge.A);
                float length = along.magnitude, from = length > Least ? Vector3.Dot(at - edge.A, along / length) : -1f;
                float off = Vector3.Dot(at - edge.A, edge.Out);
                bool side = inside <= 0f || i == Left || i == Right;
                if (side && Vector3.Dot(outward, edge.Out) > -Facing && off <= Near && off >= -Near - inside
                    && Mathf.Abs(at.y - edge.A.y) <= Level && from >= -Near && from <= length + Near)
                    ladders[i].Add(new Vector2(Mathf.Max(0f, from - span), Mathf.Min(length, from + span)));
            }
        }

        private static bool Meet(Line[] mine, Line[] theirs)
        {
            foreach (Line edge in mine)
            {
                foreach (Line other in theirs)
                {
                    if (Overlap(edge, other) != null)
                        return true;
                }
            }
            return false;
        }

        // Where another edge lies along this one, facing it, at the same height: the span along this one; null for none.
        private static Vector2? Overlap(Line edge, Line other)
        {
            Vector3 apart = other.A - edge.A;
            if (Vector3.Dot(edge.Out, other.Out) > Facing || Mathf.Abs(Vector3.Dot(apart, edge.Out)) > Near || Mathf.Abs(apart.y) > Level)
                return null;
            Vector3 way = Flat(edge.B - edge.A);
            float length = way.magnitude;
            if (length < Least)
                return null;
            way /= length;
            float a = Vector3.Dot(other.A - edge.A, way), b = Vector3.Dot(other.B - edge.A, way);
            float from = Mathf.Max(0f, Mathf.Min(a, b)), to = Mathf.Min(length, Mathf.Max(a, b));
            return to - from > Least ? new Vector2(from, to) : (Vector2?)null;
        }

        // Each edge's spans and ladders in order, overlapping ones merged, the openings both together, and the sides' key.
        private void Close()
        {
            StringBuilder key = new StringBuilder();
            for (int i = 0; i < spans.Length; i++)
            {
                Order(spans[i]);
                Order(ladders[i]);
                openings[i].AddRange(spans[i]);
                openings[i].AddRange(ladders[i]);
                Order(openings[i]);
                if (i != Left && i != Right)
                    continue;
                foreach (Vector2 span in openings[i])
                    key.Append(i).Append(':').Append(span.x.ToString("F2", Inv)).Append('-').Append(span.y.ToString("F2", Inv)).Append(';');
            }
            ShapeKey = key.ToString();
        }

        private static void Order(List<Vector2> list)
        {
            list.Sort((p, q) => p.x.CompareTo(q.x));
            Merge(list);
        }

        private static void Merge(List<Vector2> sorted)
        {
            for (int i = sorted.Count - 1; i > 0; i--)
            {
                if (sorted[i].x > sorted[i - 1].y + Least)
                    continue;
                sorted[i - 1] = new Vector2(sorted[i - 1].x, Mathf.Max(sorted[i - 1].y, sorted[i].y));
                sorted.RemoveAt(i);
            }
        }

        /// <summary>A built section's four edges at its deck's top, in the world (null for one not built or gone).</summary>
        private static Line[]? Lines(KwPier? pier)
        {
            KwPierSpec? spec = pier != null ? pier.Spec : null;
            if (spec == null)
                return null;
            Transform t = pier!.transform;
            Vector3 s = t.position, f = Flat(t.forward).normalized, right = Flat(t.right).normalized;
            Vector3 r = right * (spec.Width * 0.5f), e = s + f * spec.Length;
            return new[] { new Line(s - r, e - r, -right), new Line(s + r, e + r, right), new Line(s - r, s + r, -f), new Line(e - r, e + r, f) };
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>One deck edge: its two ends at the deck's top and the level way out of the deck across it.</summary>
        private readonly struct Line
        {
            public Line(Vector3 a, Vector3 b, Vector3 outward)
            {
                (A, B, Out) = (a, b, outward);
            }

            public Vector3 A { get; }
            public Vector3 B { get; }
            public Vector3 Out { get; }
        }
    }
}
