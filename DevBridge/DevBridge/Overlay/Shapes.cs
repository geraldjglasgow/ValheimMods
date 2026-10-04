using System.Collections.Generic;
using DevBridge.Hitbox;
using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// Wire shapes as one continuous run of points each (one pooled line per shape): boxes, spheres, capsules, rings,
    /// wedges and squares. Where a shape's edges cannot be walked without lifting the pen, an edge is walked twice.
    /// </summary>
    internal static class Shapes
    {
        private const float Tau = Mathf.PI * 2f;

        // Every edge of a box, corners 0-3 on the bottom and 4-7 above them; the last few retrace an edge to reach the next.
        private static readonly int[] BoxWalk = { 0, 1, 2, 3, 0, 4, 5, 6, 7, 4, 5, 1, 2, 6, 7, 3 };

        internal static Vector3[] Box(Vector3 centre, Vector3 half, Quaternion rotation)
        {
            var corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float x = i == 1 || i == 2 || i == 5 || i == 6 ? 1f : -1f, z = i % 4 >= 2 ? 1f : -1f;
                corners[i] = centre + rotation * Vector3.Scale(half, new Vector3(x, i >= 4 ? 1f : -1f, z));
            }
            var points = new Vector3[BoxWalk.Length];
            for (int i = 0; i < points.Length; i++) points[i] = corners[BoxWalk[i]];
            return points;
        }

        /// <summary>Three great circles; the second runs on a quarter turn so the third starts where it ends.</summary>
        internal static Vector3[] Sphere(Vector3 centre, float radius, Quaternion rotation)
        {
            Vector3 x = rotation * Vector3.right, y = rotation * Vector3.up, z = rotation * Vector3.forward;
            var points = new List<Vector3>();
            Arc(points, centre, x, z, radius, Tau, 24);
            Arc(points, centre, x, y, radius, Tau * 1.25f, 30);
            Arc(points, centre, y, z, radius, Tau, 24);
            return points.ToArray();
        }

        /// <summary>
        /// A capsule along axis, its hemisphere centres half apart from the centre: the outline in each of the two planes
        /// through the axis, and a ring round each hemisphere's rim.
        /// </summary>
        internal static Vector3[] Capsule(Vector3 centre, Vector3 axis, Vector3 a, Vector3 b, float radius, float half)
        {
            Vector3 bottom = centre - axis * half, top = centre + axis * half;
            var points = new List<Vector3>();
            Arc(points, bottom, a, -axis, radius, Mathf.PI, 12);
            Arc(points, top, -a, axis, radius, Mathf.PI, 12);
            Arc(points, bottom, a, b, radius, Tau * 1.25f, 30);
            Arc(points, bottom, b, -axis, radius, Mathf.PI, 12);
            Arc(points, top, -b, axis, radius, Mathf.PI, 12);
            Arc(points, top, b, a, radius, Tau, 24);
            points.Add(bottom + b * radius);
            return points.ToArray();
        }

        /// <summary>A flat ring with more points the larger it is.</summary>
        internal static Vector3[] Ring(Vector3 centre, float radius) =>
            Lines.Ring(centre, radius, Mathf.Clamp(Mathf.CeilToInt(radius * 1.5f), 16, 128));

        /// <summary>A flat wedge: from the origin out to the arc halfAngle degrees either side of forward, and back.</summary>
        internal static Vector3[] Wedge(Vector3 origin, Vector3 forward, float halfAngle, float radius)
        {
            Vector3 flat = Vector3.ProjectOnPlane(forward, Vector3.up);
            flat = flat.sqrMagnitude < 1e-4f ? Vector3.forward : flat.normalized;
            int steps = Mathf.Clamp(Mathf.CeilToInt(halfAngle / 4f), 2, 64);
            var points = new Vector3[steps * 2 + 3];
            points[0] = points[points.Length - 1] = origin;
            for (int i = 0; i <= steps * 2; i++)
            {
                float angle = -halfAngle + halfAngle * i / steps;
                points[i + 1] = origin + Quaternion.Euler(0f, angle, 0f) * flat * radius;
            }
            return points;
        }

        /// <summary>A flat, axis-aligned square of half side half round a point, closed.</summary>
        internal static Vector3[] Square(Vector3 centre, float half) => new[]
        {
            centre + new Vector3(-half, 0f, -half), centre + new Vector3(half, 0f, -half), centre + new Vector3(half, 0f, half),
            centre + new Vector3(-half, 0f, half), centre + new Vector3(-half, 0f, -half),
        };

        // Points round centre at radius from angle 0 (along x) to the given angle (towards y), the first one included.
        private static void Arc(List<Vector3> into, Vector3 centre, Vector3 x, Vector3 y, float radius, float angle, int steps)
        {
            for (int i = 0; i <= steps; i++)
            {
                float t = angle * i / steps;
                into.Add(centre + (x * Mathf.Cos(t) + y * Mathf.Sin(t)) * radius);
            }
        }
    }
}
