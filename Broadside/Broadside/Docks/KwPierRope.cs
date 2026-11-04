using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Rope for the rough wood docks' lashings (<see cref="KwPierRough"/>, the dock ladder's): a thin round tube along a path
    /// of points, open or closed, its rings carried along the path without twisting (each ring's frame turned only as much
    /// as the path bends), sharing its vertices so it is smooth; the rope strip (<see cref="KwPierUv.Rope"/>) once round it
    /// and along it at the atlas's density. Also the paths a lashing is made of: rings round a pole and a loop over a beam.
    /// Plain geometry into <see cref="KwPierMesh"/>.
    /// </summary>
    public static class KwPierRope
    {
        public const float Thick = 0.012f;
        private const int Sides = 5;

        /// <summary>A rope along the path (closed: back to its first point), on its own place in the rope strips.</summary>
        public static void Tube(KwPierMesh mesh, List<Vector3> path, bool closed, KwPierUv uv)
        {
            if (closed)
                path.Add(path[0]);
            float length = 0f;
            for (int i = 1; i < path.Count; i++)
                length += Vector3.Distance(path[i - 1], path[i]);
            Vector3 place = uv.Rope(length);
            Vector3 normal = Perpendicular(Tangent(path, 0, closed));
            int first = mesh.Count;
            float along = 0f;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 t = Tangent(path, i, closed);
                normal = (normal - t * Vector3.Dot(normal, t)).normalized;
                along += i > 0 ? Vector3.Distance(path[i - 1], path[i]) : 0f;
                Ring(mesh, path[i], normal, Vector3.Cross(t, normal), along, uv, place);
            }
            Stitch(mesh, first, path.Count);
        }

        // One ring of vertices round a point of the path (the last repeats the first round it, for the texture's seam).
        private static void Ring(KwPierMesh mesh, Vector3 at, Vector3 n, Vector3 b, float along, KwPierUv uv, Vector3 place)
        {
            for (int j = 0; j <= Sides; j++)
            {
                float angle = j * 2f * Mathf.PI / Sides;
                Vector3 side = n * Mathf.Cos(angle) + b * Mathf.Sin(angle);
                mesh.Vertex(at + side * Thick, side, uv.Round((float)j / Sides, along, place));
            }
        }

        // The rings joined into quads, wound to face out (tested on the first).
        private static void Stitch(KwPierMesh mesh, int first, int rings)
        {
            List<Vector3> points = mesh.Raw().Points;
            int per = Sides + 1;
            Vector3 centre = Vector3.zero;
            for (int j = 0; j < Sides; j++)
                centre += points[first + j] / Sides;
            Vector3 p0 = points[first], p1 = points[first + per], p2 = points[first + per + 1];
            bool flip = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), p0 - centre) < 0f;
            for (int i = 0; i + 1 < rings; i++)
            {
                for (int j = 0; j < Sides; j++)
                {
                    int a = first + i * per + j, b = a + per, c = b + 1, d = a + 1;
                    if (flip)
                        (b, d) = (d, b);
                    mesh.Triangle(a, b, c);
                    mesh.Triangle(a, c, d);
                }
            }
        }

        // The path's way at a point: the mean of the ways in and out of it (round the joint of a closed path).
        private static Vector3 Tangent(List<Vector3> path, int i, bool closed)
        {
            int last = path.Count - 1;
            Vector3 before = i > 0 ? path[i] - path[i - 1] : closed ? path[last] - path[last - 1] : Vector3.zero;
            Vector3 after = i < last ? path[i + 1] - path[i] : closed ? path[1] - path[0] : Vector3.zero;
            Vector3 way = before.normalized + after.normalized;
            return way.sqrMagnitude > 1e-8f ? way.normalized : (after.sqrMagnitude > 0f ? after : before).normalized;
        }

        private static Vector3 Perpendicular(Vector3 t) => Vector3.Cross(t, Mathf.Abs(t.y) < 0.9f ? Vector3.up : Vector3.right).normalized;

        /// <summary>A ring of rope round a vertical pole: its centre line at a radius round a point, tilted a little (rise across it).</summary>
        public static List<Vector3> Round(Vector3 centre, float radius, float rise, int points = 9)
        {
            List<Vector3> ring = new List<Vector3>(points + 1);
            for (int i = 0; i < points; i++)
            {
                float angle = i * 2f * Mathf.PI / points;
                ring.Add(centre + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * rise, Mathf.Sin(angle) * radius));
            }
            return ring;
        }
    }
}
