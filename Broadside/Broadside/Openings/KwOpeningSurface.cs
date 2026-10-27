using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A wall an opening is cut in, as the opening's frame and leaves are placed on it (<see cref="KwOpeningFrames"/>,
    /// <see cref="KwOpeningLeaves"/>): a point of it at (u along, z up) and a depth out of its outer face (less than 0 in
    /// through it), the way out of it there, how thick it is and how far its outer face stands proud at most (planking's
    /// laps), and its lowest and highest z at a place along. Kit axes of the part. Two kinds: a hull's planking or a cabin's
    /// raised side (<see cref="KwPlankSurface"/>) and a cabin's own wall (<see cref="KwWallSurface"/>).
    /// </summary>
    public abstract class KwOpeningSurface
    {
        protected KwOpeningSurface(string wall) => Wall = wall;

        /// <summary>Which wall of its part it is (<see cref="KwOpening.Wall"/>).</summary>
        public string Wall { get; }

        public abstract float Thick { get; }
        public abstract float Proud { get; }

        public abstract Vector3 Point(float u, float z, float depth);

        public abstract (float Lo, float Hi) Range(float u);

        /// <summary>Where the wall runs from and to along it.</summary>
        public abstract (float From, float To) Reach { get; }

        /// <summary>The way out of the wall at a point (kit, a unit); an opening's tunnel's own way (<see cref="KwOpeningTunnel"/>).</summary>
        public virtual Vector3 Out(float u, float z)
        {
            Vector3 along = Point(u + 0.02f, z, 0f) - Point(u - 0.02f, z, 0f), up = Point(u, z + 0.02f, 0f) - Point(u, z - 0.02f, 0f);
            Vector3 n = Vector3.Cross(up, along).normalized;
            Vector3 probe = Point(u, z, 0.05f) - Point(u, z, 0f);
            return Vector3.Dot(n, probe) >= 0f ? n : -n;
        }

        /// <summary>The way up the wall at a point, square to its way out (kit, a unit).</summary>
        public Vector3 Up(float u, float z)
        {
            Vector3 n = Out(u, z), up = Point(u, z + 0.02f, 0f) - Point(u, z - 0.02f, 0f);
            return Vector3.ProjectOnPlane(up, n).normalized;
        }

        /// <summary>The way along the wall (u growing) at a point, square to its way out and its way up.</summary>
        public Vector3 Along(float u, float z)
        {
            Vector3 along = Point(u + 0.02f, z, 0f) - Point(u - 0.02f, z, 0f);
            return Vector3.ProjectOnPlane(along, Out(u, z)).normalized;
        }

        /// <summary>
        /// A point of an opening's own outline (<see cref="KwOpeningShape"/>: true metres along from its middle and up from
        /// its sill, set about the opening's u and sill) as the wall's (u, z): first up the face from the sill at the
        /// opening's u along its middle line (<see cref="Upright"/>), then along the face at that height, so an opening is
        /// its true size wherever the wall curves, its sill and head level and its sides plumb.
        /// </summary>
        public Vector2 Fit(KwOpening o, Vector2 own)
        {
            Vector2 at = Upright(o.U, o.Sill, own.y - o.Sill);
            return new Vector2(Run(at.x, at.y, own.x - o.U), at.y);
        }

        /// <summary>The height reached going a distance up the face (down for less than 0) from a height at u.</summary>
        public virtual float Rise(float u, float z, float metres) => z + metres;

        /// <summary>Where (u, z) a line going a distance up the face from a point (down for less than 0) reaches: straight up at u.</summary>
        public virtual Vector2 Upright(float u, float z, float metres) => new Vector2(u, Rise(u, z, metres));

        /// <summary>The u reached going a distance along the face (back for less than 0) from u at a height.</summary>
        public virtual float Run(float u, float z, float metres) => u + metres;

        /// <summary>How far along the face it is from u0 to u1 at a height (less than 0 going back).</summary>
        public virtual float Metres(float u0, float u1, float z) => u1 - u0;
    }

    /// <summary>
    /// A hull's planking on one side (or a cabin's raised side, its sections straight up from the sheer): its sections'
    /// outside line (<see cref="KwPlanks.Base"/>) at the height z, between the stations either side of u (the part's x),
    /// its outside face Lap proud at most, its inside <see cref="KwPlanks.Thick"/> in. Above the sheer it is carried straight
    /// up (a doorway cut down from above it). A depth goes the way the strakes' own faces stand off their line: square to
    /// the sections, blended between their points and between the stations (<see cref="KwStrakes"/>), so a frame's lining
    /// runs through the wall exactly where the planking's faces are cut, on a curving bow as on a flat side.
    /// </summary>
    public sealed class KwPlankSurface : KwOpeningSurface
    {
        private readonly List<Station> stations;
        private readonly float side;

        public KwPlankSurface(string wall, List<Station> stations, float side) : base(wall) => (this.stations, this.side) = (stations, side);

        public override float Thick => KwPlanks.Thick;
        public override float Proud => KwPlanks.Lap;
        public override (float From, float To) Reach => (stations[0].X, stations[stations.Count - 1].X);

        public override Vector3 Point(float u, float z, float depth)
        {
            (int i, float t) = Between(u);
            float y = Mathf.Lerp(Across(stations[i].Half, z), Across(stations[i + 1].Half, z), t);
            Vector3 at = new Vector3(u, side * y, z);
            if (Mathf.Abs(depth) < 1e-6f)
                return at;
            return at + Fibre(i, t, z) * depth;
        }

        /// <summary>The way a depth goes from the face line at a point (kit, a unit): the sections' own, as the strakes stand off.</summary>
        public Vector3 Fibre(float u, float z)
        {
            (int i, float t) = Between(u);
            return Fibre(i, t, z);
        }

        private Vector3 Fibre(int i, float t, float z) => Vector3.Lerp(Way(stations[i], z), Way(stations[i + 1], z), t).normalized;

        // The planking's way out at a height on a station's section, blended between its points as the strakes' faces are.
        private Vector3 Way(Station st, float z)
        {
            Vector2[] half = st.Half;
            int j = 0;
            while (j + 2 < half.Length && z > half[j + 1].y)
                j++;
            float rise = half[j + 1].y - half[j].y, f = rise > 1e-5f ? Mathf.Clamp01((z - half[j].y) / rise) : 0f;
            return Vector3.Lerp(KwPlanks.Normal(st, j, side), KwPlanks.Normal(st, j + 1, side), f).normalized;
        }

        public override (float Lo, float Hi) Range(float u)
        {
            (int i, float t) = Between(u);
            Vector2[] a = stations[i].Half, c = stations[i + 1].Half;
            return (Mathf.Lerp(a[0].y, c[0].y, t), Mathf.Lerp(a[a.Length - 1].y, c[c.Length - 1].y, t));
        }

        /// <summary>Up the face at u: in steps of 2 cm of height, each as long as the face runs over it.</summary>
        public override float Rise(float u, float z, float metres)
        {
            float way = Mathf.Sign(metres), left = Mathf.Abs(metres);
            Vector3 at = Point(u, z, 0f);
            for (int i = 0; i < 2000 && left > 1e-6f; i++)
            {
                Vector3 next = Point(u, z + way * RiseStep, 0f);
                float run = Vector3.Distance(at, next);
                if (run >= left)
                    return z + way * RiseStep * left / run;
                (left, z, at) = (left - run, z + way * RiseStep, next);
            }
            return z + way * left;
        }

        private const float RiseStep = 0.02f;

        /// <summary>
        /// Up the face a distance from a point, plumb: in the upright plane through it square to the face's level way along
        /// there, in steps of 2 cm of height, each as long as the face runs over it. Where the hull flares and turns toward
        /// a stem at once, a line straight up at u leans across the face; this one stands plumb seen straight on. On a side
        /// running fore and aft, or standing upright, it is the line straight up at u.
        /// </summary>
        public override Vector2 Upright(float u, float z, float metres)
        {
            Vector3 through = Point(u, z, 0f), at = through, level = (Point(u + 0.02f, z, 0f) - Point(u - 0.02f, z, 0f)).normalized;
            float way = Mathf.Sign(metres), left = Mathf.Abs(metres);
            for (int i = 0; i < 2000 && left > 1e-6f; i++)
            {
                float zn = z + way * RiseStep, un = Plumb(u, zn, through, level);
                Vector3 next = Point(un, zn, 0f);
                float run = Vector3.Distance(at, next);
                if (run >= left)
                    return Vector2.Lerp(new Vector2(u, z), new Vector2(un, zn), left / run);
                (left, u, z, at) = (left - run, un, zn, next);
            }
            return new Vector2(u, z + way * left);
        }

        // The u at a height where the face meets the upright plane through a point square to a level way (from u, two
        // Newton steps; the face runs nearly straight over a step).
        private float Plumb(float u, float z, Vector3 through, Vector3 level)
        {
            for (int i = 0; i < 2; i++)
            {
                float off = Vector3.Dot(Point(u, z, 0f) - through, level);
                if (Mathf.Abs(off) < 1e-5f)
                    break;
                float slope = Vector3.Dot(Point(u + 0.01f, z, 0f) - Point(u - 0.01f, z, 0f), level) / 0.02f;
                if (Mathf.Abs(slope) < 1e-4f)
                    break;
                u -= off / slope;
            }
            return u;
        }

        /// <summary>Along the face at a height: straight between the stations there, so measured gap by gap.</summary>
        public override float Run(float u, float z, float metres)
        {
            float way = Mathf.Sign(metres), left = Mathf.Abs(metres);
            for (int i = 0; i < stations.Count + 1 && left > 1e-6f; i++)
            {
                float end = Next(u, way);
                if (float.IsInfinity(end))
                    return u + way * left;
                float run = Mathf.Max(Vector3.Distance(Point(u, z, 0f), Point(end, z, 0f)), Mathf.Abs(end - u));
                if (run >= left)
                    return u + (end - u) * left / run;
                (left, u) = (left - run, end);
            }
            return u + way * left;
        }

        /// <summary>The face's length from u0 to u1 at a height, station gap by station gap.</summary>
        public override float Metres(float u0, float u1, float z)
        {
            float way = Mathf.Sign(u1 - u0), sum = 0f;
            for (int i = 0; i < stations.Count + 1 && way * (u1 - u0) > 1e-6f; i++)
            {
                float end = Next(u0, way);
                end = way > 0f ? Mathf.Min(end, u1) : Mathf.Max(end, u1);
                sum += Vector3.Distance(Point(u0, z, 0f), Point(end, z, 0f));
                u0 = end;
            }
            return way * sum;
        }

        // The next station's x past u going one way; infinity past the last (the face carried straight on beyond it).
        private float Next(float u, float way)
        {
            for (int i = way > 0f ? 0 : stations.Count - 1; i >= 0 && i < stations.Count; i += way > 0f ? 1 : -1)
            {
                if (way * (stations[i].X - u) > 1e-5f)
                    return stations[i].X;
            }
            return way * float.PositiveInfinity;
        }

        // The stations either side of u and how far from the first to the second it lies.
        private (int I, float T) Between(float u)
        {
            int i = 0;
            while (i + 2 < stations.Count && u > stations[i + 1].X)
                i++;
            float span = stations[i + 1].X - stations[i].X;
            return (i, span > 1e-5f ? Mathf.Clamp01((u - stations[i].X) / span) : 0f);
        }

        /// <summary>How far out a section's line is at height z (its keel's below it, its sheer's straight up above it).</summary>
        public static float Across(Vector2[] half, float z)
        {
            if (z <= half[0].y)
                return half[0].x;
            for (int j = 0; j + 1 < half.Length; j++)
            {
                if (z <= half[j + 1].y)
                {
                    float rise = half[j + 1].y - half[j].y;
                    return rise > 1e-5f ? Mathf.Lerp(half[j].x, half[j + 1].x, (z - half[j].y) / rise) : half[j + 1].x;
                }
            }
            return half[half.Length - 1].x;
        }
    }

    /// <summary>A cabin's own wall (<see cref="KwCabinWall"/>): its outer face at depth 0, its inside <see cref="KwCabinWall.Thick"/> in.</summary>
    public sealed class KwWallSurface : KwOpeningSurface
    {
        private readonly KwCabinWall wall;

        public KwWallSurface(KwCabinWall wall) : base(wall.Name) => this.wall = wall;

        public override float Thick => KwCabinWall.Thick;
        public override float Proud => 0f;
        public override (float From, float To) Reach => (0f, wall.Length);

        public override Vector3 Point(float u, float z, float depth) => wall.At(u, z, -depth);

        /// <summary>The outline's lowest and highest z where a line straight up at u crosses it.</summary>
        public override (float Lo, float Hi) Range(float u)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            List<Vector2> outline = wall.Outline;
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 a = outline[i], c = outline[(i + 1) % outline.Count];
                if ((a.x - u) * (c.x - u) > 0f || Mathf.Abs(c.x - a.x) < 1e-6f && Mathf.Abs(a.x - u) > 1e-4f)
                    continue;
                float z = Mathf.Abs(c.x - a.x) < 1e-6f ? Mathf.Max(a.y, c.y) : Mathf.Lerp(a.y, c.y, (u - a.x) / (c.x - a.x));
                float z2 = Mathf.Abs(c.x - a.x) < 1e-6f ? Mathf.Min(a.y, c.y) : z;
                (lo, hi) = (Mathf.Min(lo, z2), Mathf.Max(hi, z));
            }
            return lo <= hi ? (lo, hi) : (0f, 0f);
        }
    }
}
