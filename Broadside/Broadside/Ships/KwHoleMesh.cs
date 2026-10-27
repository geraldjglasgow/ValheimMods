using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A mesh as plain lists, for cutting holes into it away from Unity's (<see cref="KwHoleCut"/>): points, normals,
    /// tangents, the first UVs and colours (a channel left empty when the mesh has none) and each submesh's triangles.
    /// <see cref="KwHoleMeshes"/> reads it from a Unity mesh and writes it back. Pure.
    /// </summary>
    public sealed class KwHoleMesh
    {
        public List<Vector3> Points { get; } = new List<Vector3>();
        public List<Vector3> Normals { get; } = new List<Vector3>();
        public List<Vector4> Tangents { get; } = new List<Vector4>();
        public List<Vector2> Uvs { get; } = new List<Vector2>();
        public List<Color> Colours { get; } = new List<Color>();
        public List<List<int>> Subs { get; } = new List<List<int>>();

        public int Count => Points.Count;

        public KwHoleVertex Vertex(int i) => new KwHoleVertex(Points[i], i < Normals.Count ? Normals[i] : Vector3.zero,
            i < Tangents.Count ? Tangents[i] : Vector4.zero, i < Uvs.Count ? Uvs[i] : Vector2.zero, i < Colours.Count ? Colours[i] : Color.white);

        /// <summary>Adds a vertex, its channels kept as long as the mesh keeps them; its index.</summary>
        public int Add(KwHoleVertex v)
        {
            int count = Points.Count;
            bool normals = Normals.Count == count, uvs = Uvs.Count == count, tangents = Tangents.Count == count, colours = Colours.Count == count;
            Points.Add(v.P);
            if (normals)
                Normals.Add(v.N);
            if (uvs)
                Uvs.Add(v.Uv);
            if (tangents)
                Tangents.Add(v.T);
            if (colours)
                Colours.Add(v.C);
            return Points.Count - 1;
        }

        /// <summary>Another mesh's vertices and triangles added after these (a channel only where both keep it whole).</summary>
        public void Append(KwHoleMesh more)
        {
            int count = Count;
            Join(Normals, more.Normals, count, more.Count);
            Join(Tangents, more.Tangents, count, more.Count);
            Join(Uvs, more.Uvs, count, more.Count);
            Join(Colours, more.Colours, count, more.Count);
            Points.AddRange(more.Points);
            for (int s = 0; s < more.Subs.Count && s < Subs.Count; s++)
            {
                foreach (int i in more.Subs[s])
                    Subs[s].Add(i + count);
            }
        }

        private static void Join<T>(List<T> mine, List<T> more, int count, int moreCount)
        {
            if (mine.Count == count && more.Count == moreCount)
                mine.AddRange(more);
        }

        /// <summary>A new mesh with these vertices and no triangles, its submeshes empty.</summary>
        public KwHoleMesh Vertices()
        {
            KwHoleMesh copy = new KwHoleMesh();
            copy.Points.AddRange(Points);
            copy.Normals.AddRange(Normals);
            copy.Tangents.AddRange(Tangents);
            copy.Uvs.AddRange(Uvs);
            copy.Colours.AddRange(Colours);
            foreach (List<int> _ in Subs)
                copy.Subs.Add(new List<int>());
            return copy;
        }
    }

    /// <summary>One vertex of a <see cref="KwHoleMesh"/> with all its channels, and one between two.</summary>
    public readonly struct KwHoleVertex
    {
        public KwHoleVertex(Vector3 p, Vector3 n, Vector4 t, Vector2 uv, Color c) => (P, N, T, Uv, C) = (p, n, t, uv, c);

        public Vector3 P { get; }
        public Vector3 N { get; }
        public Vector4 T { get; }
        public Vector2 Uv { get; }
        public Color C { get; }

        public KwHoleVertex With(Vector3 p, Vector3 n) => new KwHoleVertex(p, n, T, Uv, C);

        public KwHoleVertex WithUv(Vector2 uv) => new KwHoleVertex(P, N, T, uv, C);

        /// <summary>The vertex a share f of the way from a to b, its normal and tangent kept unit.</summary>
        public static KwHoleVertex Lerp(KwHoleVertex a, KwHoleVertex b, float f)
        {
            Vector3 n = Vector3.Lerp(a.N, b.N, f), t = Vector3.Lerp(a.T, b.T, f);
            return new KwHoleVertex(Vector3.LerpUnclamped(a.P, b.P, f), n.sqrMagnitude > 1e-12f ? n.normalized : a.N,
                t.sqrMagnitude > 1e-12f ? (Vector4)t.normalized + new Vector4(0f, 0f, 0f, a.T.w) : a.T, Vector2.LerpUnclamped(a.Uv, b.Uv, f), Color.Lerp(a.C, b.C, f));
        }
    }
}
