using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// An edge a ladder may stand against, in the world (<see cref="KwLadderEdges"/>): a run of straight stretches along the
    /// top of a deck or a roof (a deck's end straight across; a cabin roof's side, along the hull's line in a stretch a
    /// section), each with its level way out over the drop; its piece, whether it is open (no other deck carries on from
    /// it) and what each end of it is in the player's words (<see cref="KwLadderBound.HullSide"/>,
    /// <see cref="KwLadderBound.Corner"/>). On a launched ship (<see cref="KwSeaLadderEdges"/>) it is in the ship's frame,
    /// with no piece but the part's place in the blueprint. A place along it is metres from its start. Pure numbers but its piece.
    /// </summary>
    public sealed class KwLadderEdge
    {
        private readonly List<Vector3> points;
        private readonly List<Vector3> outs;
        private readonly List<float> marks = new List<float> { 0f };

        /// <summary>An edge through two or more points (one way out a stretch), its piece (or its part's place in a launched
        /// ship's blueprint), whether it is open and its ends' names.</summary>
        public KwLadderEdge(List<Vector3> points, List<Vector3> outwards, Piece? piece, bool open, string start, string end, int part = -1)
        {
            (this.points, outs, Piece, Open, StartName, EndName, Part) = (points, outwards, piece, open, start, end, part);
            for (int i = 1; i < points.Count; i++)
                marks.Add(marks[i - 1] + Vector3.Distance(points[i - 1], points[i]));
        }

        /// <summary>The dock's piece it is an edge of (null on a launched ship).</summary>
        public Piece? Piece { get; }

        /// <summary>On a launched ship, the place in its blueprint of the deck or cabin it is an edge of; -1 on the dock.</summary>
        public int Part { get; }

        /// <summary>No other deck carries on from it at its height.</summary>
        public bool Open { get; }

        /// <summary>What its start and its end are: a hull side, a corner.</summary>
        public string StartName { get; }
        public string EndName { get; }

        public float Length => marks[marks.Count - 1];
        public Vector3 Middle => At(Length / 2f);

        /// <summary>The point s along it.</summary>
        public Vector3 At(float s)
        {
            int i = Stretch(s);
            float span = marks[i + 1] - marks[i];
            return Vector3.Lerp(points[i], points[i + 1], span > 1e-5f ? (s - marks[i]) / span : 0f);
        }

        /// <summary>The level way out over the drop s along it.</summary>
        public Vector3 OutwardAt(float s) => outs[Mathf.Min(Stretch(s), outs.Count - 1)];

        /// <summary>The level way along it (s growing) at s.</summary>
        public Vector3 WayAt(float s)
        {
            int i = Stretch(s);
            Vector3 run = Flat(points[i + 1] - points[i]);
            return run.sqrMagnitude > 1e-8f ? run.normalized : Vector3.zero;
        }

        /// <summary>How far along it its point nearest a point lies, across the ground.</summary>
        public float Along(Vector3 point) => Nearest(point).S;

        /// <summary>How far a point is from it, across the ground.</summary>
        public float Off(Vector3 point) => Nearest(point).Off;

        // The stretch s falls in.
        private int Stretch(float s)
        {
            for (int i = 0; i + 2 < marks.Count; i++)
            {
                if (s <= marks[i + 1])
                    return i;
            }
            return marks.Count - 2;
        }

        // Its point nearest a point across the ground: how far along it, and how far off.
        private (float S, float Off) Nearest(Vector3 point)
        {
            (float S, float Off) best = (0f, float.MaxValue);
            for (int i = 0; i + 1 < points.Count; i++)
            {
                Vector3 run = Flat(points[i + 1] - points[i]), from = Flat(point - points[i]);
                float t = run.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector3.Dot(from, run) / run.sqrMagnitude) : 0f;
                float off = (from - run * t).magnitude;
                if (off < best.Off)
                    best = (marks[i] + (marks[i + 1] - marks[i]) * t, off);
            }
            return best;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
