using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One run of a roof railing (<see cref="KwRoofRailShape"/>): its points end to end, how far along it each stands, the
    /// way into the roof from each edge, and its gaps as stretches along it (<see cref="KwRoofRail.Gap"/> wide round each
    /// place a ladder comes up that lies on it). Kit x forward, y to port. Pure.
    /// </summary>
    public sealed class KwRoofRun
    {
        /// <summary>How near a run a ladder's place must lie to cut a gap in it.</summary>
        public const float Reach = 0.25f;

        private readonly Vector2 middle;

        public KwRoofRun(List<Vector2> points, Vector2 middle, IEnumerable<Vector2> gaps)
        {
            Points = points;
            this.middle = middle;
            Along = new List<float> { 0f };
            for (int i = 1; i < points.Count; i++)
                Along.Add(Along[i - 1] + (points[i] - points[i - 1]).magnitude);
            foreach (Vector2 gap in gaps)
            {
                float? at = Place(gap);
                if (at != null)
                    Gaps.Add(new Vector2(Mathf.Max(0f, at.Value - KwRoofRail.Gap / 2f), Mathf.Min(Length, at.Value + KwRoofRail.Gap / 2f)));
            }
        }

        public List<Vector2> Points { get; }

        /// <summary>How far along the run each point stands.</summary>
        public List<float> Along { get; }
        public float Length => Along[Along.Count - 1];

        /// <summary>The gaps, each from x to y along the run.</summary>
        public List<Vector2> Gaps { get; } = new List<Vector2>();

        /// <summary>How far along the run a point on it lies (within <see cref="Reach"/> of it); null for one off it.</summary>
        public float? Place(Vector2 point)
        {
            float? best = null;
            float nearest = Reach;
            for (int i = 0; i + 1 < Points.Count; i++)
            {
                KwRoofSegment edge = new KwRoofSegment('f', Points[i], Points[i + 1]);
                Vector2 on = edge.Nearest(point);
                float off = (on - point).magnitude;
                if (off > nearest)
                    continue;
                nearest = off;
                best = Along[i] + (on - Points[i]).magnitude;
            }
            return best;
        }

        /// <summary>The run's ends and the points where it turns by more than an angle, in order.</summary>
        public List<int> Corners(float angle)
        {
            List<int> corners = new List<int> { 0 };
            for (int i = 1; i + 1 < Points.Count; i++)
            {
                if (Vector2.Angle(Way(i - 1), Way(i)) > angle)
                    corners.Add(i);
            }
            corners.Add(Points.Count - 1);
            return corners;
        }

        /// <summary>The stretches of edge i (from, to, along the run) left once its gaps are cut out.</summary>
        public List<Vector2> Pieces(int i)
        {
            List<Vector2> pieces = new List<Vector2> { new Vector2(Along[i], Along[i + 1]) };
            foreach (Vector2 gap in Gaps)
            {
                List<Vector2> cut = new List<Vector2>();
                foreach (Vector2 piece in pieces)
                {
                    if (gap.x > piece.x + 0.01f)
                        cut.Add(new Vector2(piece.x, Mathf.Min(piece.y, gap.x)));
                    if (gap.y < piece.y - 0.01f)
                        cut.Add(new Vector2(Mathf.Max(piece.x, gap.y), piece.y));
                }
                pieces = cut.FindAll(p => p.y - p.x > 0.01f);
            }
            return pieces;
        }

        /// <summary>The edge a place along the run lies on.</summary>
        public int Segment(float at)
        {
            for (int i = 0; i + 2 < Points.Count; i++)
            {
                if (at <= Along[i + 1])
                    return i;
            }
            return Mathf.Max(0, Points.Count - 2);
        }

        /// <summary>The point on edge i at a place along the run.</summary>
        public Vector2 At(int i, float at)
        {
            float run = Along[i + 1] - Along[i];
            return Vector2.Lerp(Points[i], Points[i + 1], run > 1e-5f ? (at - Along[i]) / run : 0f);
        }

        /// <summary>Edge i's way along the run.</summary>
        public Vector2 Way(int i) => (Points[i + 1] - Points[i]).normalized;

        /// <summary>Square to edge i, into the roof (towards the outline's middle).</summary>
        public Vector2 Inward(int i)
        {
            Vector2 way = Way(i), square = new Vector2(-way.y, way.x);
            Vector2 mid = (Points[i] + Points[i + 1]) / 2f;
            return Vector2.Dot(square, middle - mid) >= 0f ? square : -square;
        }
    }
}
