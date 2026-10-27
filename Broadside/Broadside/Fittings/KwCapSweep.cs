using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Tubes swept along a centre line, for the post caps' curls and turned work: a ring of a cross-section
    /// round every point of the line, each ring joined to the next by quads and both ends capped, so the tube is closed.
    /// A section is given in units: its x along the line's normal in the plane the line bends in, its y across the ship
    /// (the line must never run along y); a size at each point (half depth, half width) scales it. The wood's grain runs
    /// along the line at two metres a texture. Also the spirals the curls roll in, in the ship's centre plane. Kit axes: x
    /// forward, y left, z up, metres.
    /// </summary>
    public static class KwCapSweep
    {
        private const float Grain = 0.5f, Around = 0.25f;

        /// <summary>A carved board's section: flat sides across the ship, bevelled to a ridge fore and aft.</summary>
        public static readonly Vector2[] Bevelled =
        {
            new Vector2(1f, 0f), new Vector2(0.55f, 1f), new Vector2(-0.55f, 1f), new Vector2(-1f, 0f), new Vector2(-0.55f, -1f), new Vector2(0.55f, -1f),
        };

        /// <summary>A round section of so many sides, for turned work.</summary>
        public static Vector2[] Round(int sides)
        {
            Vector2[] round = new Vector2[sides];
            for (int i = 0; i < sides; i++)
                round[i] = new Vector2(Mathf.Cos(i * 2f * Mathf.PI / sides), Mathf.Sin(i * 2f * Mathf.PI / sides));
            return round;
        }

        /// <summary>A tube along the line, its size at each point from that point's share t of the line's length (0 to 1).</summary>
        public static void Tube(KwCapPen b, IList<Vector3> line, Func<float, Vector2> size, Vector2[] section)
        {
            float[] along = Lengths(line);
            float total = Mathf.Max(along[along.Length - 1], 1e-4f);
            List<Vector2> sizes = new List<Vector2>();
            foreach (float s in along)
                sizes.Add(size(s / total));
            Tube(b, line, sizes, section);
        }

        /// <summary>A tube along the line with a size given at every point.</summary>
        public static void Tube(KwCapPen b, IList<Vector3> line, IList<Vector2> sizes, Vector2[] section)
        {
            float[] along = Lengths(line);
            List<List<Vector3>> rings = new List<List<Vector3>>();
            for (int i = 0; i < line.Count; i++)
                rings.Add(Ring(line[i], Tangent(line, i), sizes[i], section));
            for (int i = 0; i + 1 < rings.Count; i++)
                Join(b, rings[i], rings[i + 1], along[i] * Grain, along[i + 1] * Grain);
            b.Flat(rings[0]);
            b.Flat(rings[rings.Count - 1]);
        }

        /// <summary>
        /// The points after <paramref name="start"/> along a spiral in the plane y = start.y that leaves start heading up
        /// and rolls over toward +x (side 1) or -x (side -1): its radius starts at radius and shrinks to shrink
        /// of itself each turn, going round turns times in steps points.
        /// </summary>
        public static List<Vector3> Spiral(Vector3 start, float radius, float shrink, float turns, int steps, float side)
        {
            Vector3 centre = SpiralCentre(start, radius, side);
            List<Vector3> points = new List<Vector3>();
            for (int i = 1; i <= steps; i++)
            {
                float angle = turns * 2f * Mathf.PI * i / steps;
                float r = radius * Mathf.Pow(shrink, angle / (2f * Mathf.PI));
                points.Add(centre + new Vector3(-side * Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * r);
            }
            return points;
        }

        /// <summary>The eye a spiral from start rolls round.</summary>
        public static Vector3 SpiralCentre(Vector3 start, float radius, float side) => start + new Vector3(side * radius, 0f, 0f);

        // The section round a point, square to the line there: x along its normal in the bending plane, y across.
        private static List<Vector3> Ring(Vector3 at, Vector3 tangent, Vector2 size, Vector2[] section)
        {
            Vector3 normal = Vector3.Cross(new Vector3(0f, 1f, 0f), tangent).normalized;
            Vector3 across = Vector3.Cross(tangent, normal);
            List<Vector3> ring = new List<Vector3>();
            foreach (Vector2 p in section)
                ring.Add(at + normal * (p.x * size.x) + across * (p.y * size.y));
            return ring;
        }

        // The line's direction at point i: from the point before to the point after (or the end's own segment).
        private static Vector3 Tangent(IList<Vector3> line, int i)
        {
            Vector3 before = line[Mathf.Max(i - 1, 0)], after = line[Mathf.Min(i + 1, line.Count - 1)];
            return (after - before).normalized;
        }

        // Two rings joined side by side, the grain from u0 to u1 along the line.
        private static void Join(KwCapPen b, List<Vector3> near, List<Vector3> far, float u0, float u1)
        {
            int sides = near.Count;
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                float v0 = Around * i / sides, v1 = Around * (i + 1) / sides;
                b.Polygon(new List<Vector3> { near[i], far[i], far[j], near[j] },
                    new List<Vector2> { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1) });
            }
        }

        // The distance along the line to each point.
        private static float[] Lengths(IList<Vector3> line)
        {
            float[] along = new float[line.Count];
            for (int i = 1; i < line.Count; i++)
                along[i] = along[i - 1] + Vector3.Distance(line[i - 1], line[i]);
            return along;
        }
    }
}
