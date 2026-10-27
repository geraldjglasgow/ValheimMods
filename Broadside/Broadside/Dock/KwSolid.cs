using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Simple timber solids for the dock set, in kit axes, into a <see cref="MeshBuilder"/>: boxes, boxes that follow a
    /// slope, prisms (a cross-section pushed along x) and round logs. Planar UVs at two metres a texture, so the game's
    /// plank wood tiles along each face.
    /// </summary>
    public static class Solid
    {
        // corners 0-3 the bottom ring, 4-7 the top ring above them
        private static readonly int[][] Faces =
        {
            new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 },
            new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 },
        };

        public static void Box(MeshBuilder b, Vector3 min, Vector3 max) => Hexa(b, new[]
        {
            new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z),
        });

        /// <summary>A beam from x0 to x1, y0 to y1, lying on a slope z(x): from z(x)+bottom to z(x)+top.</summary>
        public static void Sloped(MeshBuilder b, float x0, float x1, float y0, float y1, Func<float, float> z, float bottom, float top) => Hexa(b, new[]
        {
            new Vector3(x0, y0, z(x0) + bottom), new Vector3(x1, y0, z(x1) + bottom), new Vector3(x1, y1, z(x1) + bottom), new Vector3(x0, y1, z(x0) + bottom),
            new Vector3(x0, y0, z(x0) + top), new Vector3(x1, y0, z(x1) + top), new Vector3(x1, y1, z(x1) + top), new Vector3(x0, y1, z(x0) + top),
        });

        public static void Hexa(MeshBuilder b, Vector3[] corners)
        {
            foreach (int[] face in Faces)
            {
                List<Vector3> quad = face.Select(i => corners[i]).ToList();
                b.Polygon(quad, Planar(quad));
            }
        }

        /// <summary>
        /// The same with UVs of its own: uv(face, corner, point) for each face in order (underside, top, the side from
        /// corner 0 to 1, the end at corner 1, the side from corner 2 to 3, the end at corner 0), each of its corners by index.
        /// </summary>
        public static void Hexa(MeshBuilder b, Vector3[] corners, Func<int, int, Vector3, Vector2> uv)
        {
            for (int f = 0; f < Faces.Length; f++)
            {
                int[] face = Faces[f];
                b.Polygon(face.Select(i => corners[i]).ToList(), face.Select(i => uv(f, i, corners[i])).ToList());
            }
        }

        /// <summary>A cross-section (y, z) pushed along x from x0 to x1: two caps and the sides between.</summary>
        public static void Prism(MeshBuilder b, Vector2[] outline, float x0, float x1)
        {
            List<Vector3> near = outline.Select(p => new Vector3(x0, p.x, p.y)).ToList();
            List<Vector3> far = outline.Select(p => new Vector3(x1, p.x, p.y)).ToList();
            b.Polygon(near, Planar(near));
            b.Polygon(far, Planar(far));
            for (int i = 0; i < outline.Length; i++)
            {
                int j = (i + 1) % outline.Length;
                List<Vector3> side = new List<Vector3> { near[i], far[i], far[j], near[j] };
                b.Polygon(side, Planar(side));
            }
        }

        /// <summary>A round log from a to c, with its bark running along it.</summary>
        public static void Log(MeshBuilder b, Vector3 a, Vector3 c, float radius, int sides = 8)
        {
            Vector3 axis = (c - a).normalized;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.z) < 0.9f ? Vector3.forward : Vector3.right).normalized;
            Vector3 v = Vector3.Cross(axis, u);
            Vector3[] ring = Enumerable.Range(0, sides).Select(i => (u * Mathf.Cos(i * 2f * Mathf.PI / sides) + v * Mathf.Sin(i * 2f * Mathf.PI / sides)) * radius).ToArray();
            float length = (c - a).magnitude * 0.5f;
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                float s0 = (float)i / sides, s1 = (float)(i + 1) / sides;
                b.Polygon(new List<Vector3> { a + ring[i], c + ring[i], c + ring[j], a + ring[j] },
                    new List<Vector2> { new Vector2(0f, s0), new Vector2(length, s0), new Vector2(length, s1), new Vector2(0f, s1) });
            }
            foreach (Vector3 end in new[] { a, c })
            {
                List<Vector3> cap = ring.Select(r => end + r).ToList();
                b.Polygon(cap, Planar(cap));
            }
        }

        /// <summary>UVs for a flat face: its two axes nearest the face's plane, at two metres a texture.</summary>
        public static List<Vector2> Planar(List<Vector3> points)
        {
            Vector3 n = Vector3.Cross(points[1] - points[0], points[2] - points[0]);
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            return points.ConvertAll(p => (az >= ax && az >= ay ? new Vector2(p.x, p.y) : ay >= ax ? new Vector2(p.x, p.z) : new Vector2(p.y, p.z)) * 0.5f);
        }
    }
}
