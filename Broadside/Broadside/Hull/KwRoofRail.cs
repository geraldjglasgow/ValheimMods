using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>One straight edge of a cabin roof's outline as a roof railing saves it: its side's letter and its two ends (kit x forward, y to port).</summary>
    public readonly struct KwRoofSegment
    {
        public KwRoofSegment(char side, Vector2 a, Vector2 b)
        {
            Side = side;
            A = a;
            B = b;
        }

        /// <summary>'f' fore, 'a' aft, 'p' port or 's' starboard.</summary>
        public char Side { get; }
        public Vector2 A { get; }
        public Vector2 B { get; }
        public float Length => (B - A).magnitude;

        /// <summary>The point on the edge nearest a point.</summary>
        public Vector2 Nearest(Vector2 point)
        {
            Vector2 run = B - A;
            float t = run.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(point - A, run) / run.sqrMagnitude) : 0f;
            return A + run * t;
        }
    }

    /// <summary>
    /// A railing on a cabin's roof (the railing tool's click on a cabin, <see cref="KwRoofRailTool"/>): a railing part
    /// (<c>KW_gen_rail</c>) of its own, standing on the roof's top in the cabin's heading, whose saved text keeps the roof's
    /// whole outline, so an edit turns its sides on and off without the cabin: "roofrail|posts|sides|edges|gaps": its posts'
    /// spacing in metres; the sides with a rail, letters of f, a, p and s (fore, aft, port, starboard); every edge of the
    /// outline as "f:x0:y0:x1:y1", ";" between them (kit x forward and y to port, from the part's origin on the roof's top);
    /// and the places a ladder comes up, "x:y", where the rail leaves a gap <see cref="Gap"/> wide. Never a
    /// <see cref="KwSpan"/>, so the hull's measures, shield hooks and the hull railings' planking tier leave it out. Pure.
    /// </summary>
    public sealed class KwRoofRail
    {
        public const string Tag = "roofrail";

        /// <summary>How wide a gap a ladder coming up leaves: wide enough for the player's body (0.98 m) to walk through it.</summary>
        public const float Gap = 1.2f;

        /// <summary>The sides by name, as <see cref="KwCabinRoofs"/> names them, in the window's order.</summary>
        public static readonly string[] Sides = { "fore", "aft", "port", "starboard" };

        public KwRoofRail(float spacing, string on, List<KwRoofSegment> edges, List<Vector2> gaps)
        {
            Spacing = Mathf.Clamp(spacing, KwRailText.Least, KwRailText.Most);
            On = new string(Sides.Select(side => side[0]).Where(letter => (on ?? "").IndexOf(letter) >= 0).ToArray());
            Edges = edges;
            Gaps = gaps;
        }

        public float Spacing { get; }

        /// <summary>The sides with a rail, as letters in the window's order: "fps".</summary>
        public string On { get; }
        public List<KwRoofSegment> Edges { get; }
        public List<Vector2> Gaps { get; }

        public bool Has(char side) => On.IndexOf(side) >= 0;

        /// <summary>The edges with a rail.</summary>
        public List<KwRoofSegment> Railed => Edges.FindAll(edge => Has(edge.Side));

        /// <summary>How long its rails run: the railed edges' lengths, for its cost.</summary>
        public float Length => Railed.Sum(edge => edge.Length);

        public KwRoofRail With(float spacing, string on) => new KwRoofRail(spacing, on, Edges, Gaps);

        /// <summary>The same with a gap where a ladder comes up at a point on its outline (none added near one it has).</summary>
        public KwRoofRail WithGap(Vector2 at) =>
            Gaps.Any(gap => (gap - at).magnitude < 0.2f) ? this : new KwRoofRail(Spacing, On, Edges, new List<Vector2>(Gaps) { at });

        /// <summary>The point on its outline nearest a point, when within reach of it (any side, railed or not); else null.</summary>
        public Vector2? Near(Vector2 point, float reach)
        {
            Vector2? best = null;
            foreach (KwRoofSegment edge in Edges)
            {
                Vector2 on = edge.Nearest(point);
                if ((on - point).magnitude <= reach && (best == null || (on - point).sqrMagnitude < (best.Value - point).sqrMagnitude))
                    best = on;
            }
            return best;
        }

        /// <summary>Made again with the ship: x times s, y (across) times w and s; its posts' spacing kept.</summary>
        public KwRoofRail Reshaped(float w, float s)
        {
            Vector2 scale = new Vector2(s, w * s);
            List<KwRoofSegment> edges = Edges.ConvertAll(e => new KwRoofSegment(e.Side, Vector2.Scale(e.A, scale), Vector2.Scale(e.B, scale)));
            return new KwRoofRail(Spacing, On, edges, Gaps.ConvertAll(gap => Vector2.Scale(gap, scale)));
        }

        public override string ToString()
        {
            string edges = string.Join(";", Edges.Select(e => e.Side + ":" + P(e.A) + ":" + P(e.B)));
            return string.Join("|", Tag, F(Spacing), On, edges, string.Join(";", Gaps.Select(P)));
        }

        public static bool TryParse(string text, out KwRoofRail rail)
        {
            rail = null!;
            string[] p = (text ?? "").Split('|');
            if (p.Length != 5 || p[0] != Tag || !Num(p[1], out float spacing) || !ReadEdges(p[3], out List<KwRoofSegment> edges))
                return false;
            List<Vector2> gaps = new List<Vector2>();
            foreach (string gap in p[4].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] xy = gap.Split(':');
                if (xy.Length != 2 || !Num(xy[0], out float x) || !Num(xy[1], out float y))
                    return false;
                gaps.Add(new Vector2(x, y));
            }
            rail = new KwRoofRail(spacing, p[2], edges, gaps);
            return edges.Count > 0;
        }

        // The outline's edges, "f:x0:y0:x1:y1" each.
        private static bool ReadEdges(string text, out List<KwRoofSegment> edges)
        {
            edges = new List<KwRoofSegment>();
            foreach (string edge in text.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = edge.Split(':');
                if (p.Length != 5 || p[0].Length != 1 || Array.IndexOf(Letters, p[0][0]) < 0)
                    return false;
                if (!Num(p[1], out float x0) || !Num(p[2], out float y0) || !Num(p[3], out float x1) || !Num(p[4], out float y1))
                    return false;
                edges.Add(new KwRoofSegment(p[0][0], new Vector2(x0, y0), new Vector2(x1, y1)));
            }
            return true;
        }

        /// <summary>The sides' letters, in the window's order.</summary>
        public static readonly char[] Letters = Sides.Select(side => side[0]).ToArray();

        /// <summary>A side's letter from its name ("fore" 'f'); '?' for a name that is none of the four.</summary>
        public static char Letter(string side) => Array.IndexOf(Sides, side) >= 0 ? side[0] : '?';

        private static string P(Vector2 p) => F(p.x) + ":" + F(p.y);

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static bool Num(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
