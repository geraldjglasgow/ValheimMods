using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// A dock's timbers gathered into one mesh, in Unity axes in the dock's own frame (x across, y up, z along): boxes whose
    /// grain runs along one axis, round logs and poles (tapered, on any strip family), and through <see cref="Face"/>,
    /// <see cref="Vertex"/> and <see cref="Triangle"/> anything else (<see cref="KwPierBlock"/>'s crooked boards,
    /// <see cref="KwPierRope"/>'s ropes). Faces are one-sided, wound to face out, with flat normals on boxes and round ones
    /// on logs; every vertex white (as the game's piece shader reads them). <see cref="Offset"/> moves what comes next (a
    /// later section in the preview's one mesh).
    /// </summary>
    public sealed class KwPierMesh
    {
        private const int Sides = 10;
        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        public Vector3 Offset { get; set; }

        /// <summary>The vertices gathered so far.</summary>
        public int Count => points.Count;

        /// <summary>A box from min to max, its grain along an axis (0 x, 1 y, 2 z): sides in the grain, ends on the end grain.</summary>
        public void Box(Vector3 min, Vector3 max, int grain, KwPierUv uv, Vector3 shift)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                Side(min, max, axis, 1, grain, uv, shift);
                Side(min, max, axis, -1, grain, uv, shift);
            }
        }

        private void Side(Vector3 min, Vector3 max, int axis, int sign, int grain, KwPierUv uv, Vector3 shift)
        {
            int u = (axis + 1) % 3, v = (axis + 2) % 3;
            float[] us = { min[u], max[u], max[u], min[u] }, vs = { min[v], min[v], max[v], max[v] };
            Vector3[] corners = new Vector3[4];
            Vector2[] map = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = default;
                (p[axis], p[u], p[v]) = (sign > 0 ? max[axis] : min[axis], us[i], vs[i]);
                corners[i] = p;
                map[i] = FaceUv(p, min, max, axis, grain, uv, shift);
            }
            Vector3 n = default;
            n[axis] = sign;
            Face(corners, map, n);
        }

        // A side in the grain (along it from the box's corner, across the face's other axis), or an end on the end grain.
        private static Vector2 FaceUv(Vector3 p, Vector3 min, Vector3 max, int axis, int grain, KwPierUv uv, Vector3 shift)
        {
            if (axis == grain)
            {
                int a = (axis + 1) % 3, b = (axis + 2) % 3;
                Vector3 mid = (min + max) * 0.5f;
                return uv.End(p[a] - mid[a], p[b] - mid[b]);
            }
            int across = 3 - axis - grain;
            return uv.Side(p[grain] - min[grain], p[across] - min[across], shift);
        }

        /// <summary>Four corners as one face looking along n (their order turned round if it looked the other way).</summary>
        public void Face(Vector3[] c, Vector2[] t, Vector3 n)
        {
            bool flip = Vector3.Dot(Vector3.Cross(c[1] - c[0], c[2] - c[0]), n) < 0f;
            int start = points.Count;
            for (int i = 0; i < 4; i++)
            {
                int k = flip ? (4 - i) % 4 : i;
                Add(c[k], n, t[k]);
            }
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        /// <summary>A round log from a to b, its bark cut every <see cref="KwPierUv.Run"/> along; its ends capped where asked.</summary>
        public void Log(Vector3 a, Vector3 b, float radius, KwPierUv uv, bool capA, bool capB) =>
            Round(a, b, radius, radius, uv.Timber, uv, capA, capB);

        /// <summary>
        /// A round length from a to b, its radius ra at a and rb at b, cut every <see cref="KwPierUv.Run"/> along, each
        /// length on a place of its own from pick (a log's bark, a pole's); its ends capped on the end grain where asked.
        /// </summary>
        public void Round(Vector3 a, Vector3 b, float ra, float rb, Func<float, Vector3> pick, KwPierUv uv, bool capA, bool capB)
        {
            Vector3 axis = (b - a).normalized;
            Vector3 x = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized, y = Vector3.Cross(axis, x);
            float length = Vector3.Distance(a, b);
            for (float s = 0f; s < length - 0.001f; s += uv.Run)
            {
                float s1 = Mathf.Min(length, s + uv.Run);
                Bark(a, axis, x, y, new Vector4(s, s1, Mathf.Lerp(ra, rb, s / length), Mathf.Lerp(ra, rb, s1 / length)), pick(s1 - s), uv);
            }
            if (capA)
                Cap(a, -axis, x, y, ra, uv);
            if (capB)
                Cap(b, axis, x, y, rb, uv);
        }

        // One length of bark from s0 to s1 along the log (span: s0, s1 and the radius at each), on its place: a quad round it
        // per side, normals pointing out from its axis.
        private void Bark(Vector3 a, Vector3 axis, Vector3 x, Vector3 y, Vector4 span, Vector3 place, KwPierUv uv)
        {
            float run = span.y - span.x;
            for (int i = 0; i < Sides; i++)
            {
                Vector3 r0 = Ring(x, y, i), r1 = Ring(x, y, i + 1);
                int start = points.Count;
                float u0 = (float)i / Sides, u1 = (float)(i + 1) / Sides;
                Add(a + axis * span.x + r0 * span.z, r0, uv.Round(u0, 0f, place));
                Add(a + axis * span.y + r0 * span.w, r0, uv.Round(u0, run, place));
                Add(a + axis * span.y + r1 * span.w, r1, uv.Round(u1, run, place));
                Add(a + axis * span.x + r1 * span.z, r1, uv.Round(u1, 0f, place));
                bool flip = Vector3.Dot(Vector3.Cross(points[start + 1] - points[start], points[start + 2] - points[start]), r0 + r1) < 0f;
                triangles.AddRange(flip ? new[] { start, start + 2, start + 1, start, start + 3, start + 2 } : new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
        }

        // A log's end: a fan round its middle on the end grain, looking along facing.
        private void Cap(Vector3 at, Vector3 facing, Vector3 x, Vector3 y, float radius, KwPierUv uv)
        {
            int middle = points.Count;
            Add(at, facing, uv.End(0f, 0f));
            for (int i = 0; i < Sides; i++)
            {
                Vector3 r = Ring(x, y, i) * radius;
                Add(at + r, facing, uv.End(Vector3.Dot(r, x), Vector3.Dot(r, y)));
            }
            bool flip = Vector3.Dot(Vector3.Cross(Ring(x, y, 0), Ring(x, y, 1)), facing) < 0f;
            for (int i = 0; i < Sides; i++)
            {
                int p = middle + 1 + i, q = middle + 1 + (i + 1) % Sides;
                triangles.AddRange(flip ? new[] { middle, q, p } : new[] { middle, p, q });
            }
        }

        private static Vector3 Ring(Vector3 x, Vector3 y, int i)
        {
            float angle = i * 2f * Mathf.PI / Sides;
            return x * Mathf.Cos(angle) + y * Mathf.Sin(angle);
        }

        /// <summary>One vertex (moved by <see cref="Offset"/>); its number, for <see cref="Triangle"/>.</summary>
        public int Vertex(Vector3 point, Vector3 normal, Vector2 uv)
        {
            Add(point, normal, uv);
            return points.Count - 1;
        }

        /// <summary>A triangle of three vertices already added, in that order.</summary>
        public void Triangle(int a, int b, int c) => triangles.AddRange(new[] { a, b, c });

        private void Add(Vector3 point, Vector3 normal, Vector2 uv)
        {
            points.Add(point + Offset);
            normals.Add(normal.normalized);
            uvs.Add(uv);
        }

        /// <summary>The gathered vertices, normals, texture coordinates and triangles, as they are (an offline check reads them).</summary>
        public (List<Vector3> Points, List<Vector3> Normals, List<Vector2> Uvs, List<int> Triangles) Raw() => (points, normals, uvs, triangles);

        /// <summary>Everything gathered as one mesh (32-bit indices when large), with tangents for the game's normal maps.</summary>
        public Mesh ToMesh(string name)
        {
            Mesh mesh = new Mesh { name = name };
            if (points.Count > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(points);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(new List<Color>(new Color[points.Count]).ConvertAll(_ => Color.white));
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
