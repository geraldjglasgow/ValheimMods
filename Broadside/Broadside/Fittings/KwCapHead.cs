using System;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One carved head's model as read from the <c>kw_postheads</c> bundle (<see cref="KwCapHeads"/>), drawn for the
    /// standard post: its mesh's corners in its prefab's frame (Unity axes, metres: +y up, +z the way it looks, x across the
    /// ship) moved so its foot's middle is at the origin, the height of its <c>head_start</c>, where the neck ends and the
    /// head begins (every head part is above it), and its neck's middle line between (<see cref="Middle"/>). Its foot is
    /// <see cref="FootWidth"/> by <see cref="FootDepth"/> at y = 0, its moulding up to <see cref="FootTop"/>. Plain data,
    /// kept for the session; warped copies are made from it (<see cref="KwCapWarp"/>).
    /// </summary>
    public sealed class KwCapHead
    {
        /// <summary>The foot, across the ship (x) and along the keel (z), and its moulding's top, where the neck starts.</summary>
        public const float FootWidth = 0.4f, FootDepth = 0.3f, FootTop = 0.136f;

        // Corners at most this high are the foot's underside, whose middle is the origin; the neck's middle is measured in
        // bands this high, from the corners within this share of a band's widest reach across.
        private const float Underside = 1e-3f, Band = 0.04f, Side = 0.85f;
        private readonly float[] middles;

        public KwCapHead(Vector3[] vertices, Vector3[] normals, Vector4[] tangents, Vector2[] uv, int[] triangles, float start)
        {
            Vertices = Centred(vertices);
            Normals = normals;
            Tangents = tangents;
            Uv = uv;
            Triangles = triangles;
            Start = Mathf.Max(start, FootTop + Band);
            middles = Middles(Vertices, Start);
        }

        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }

        /// <summary>The tangents for its normal map (x, y, z and the bitangent's sign); empty when the model has none.</summary>
        public Vector4[] Tangents { get; }
        public Vector2[] Uv { get; }
        public int[] Triangles { get; }

        /// <summary>The height of its head_start: the neck below it stretches and bends, the head above it is moved whole.</summary>
        public float Start { get; }

        /// <summary>The neck's middle at a height (between the foot's top and head_start): how far forward of the foot's middle (z).</summary>
        public float Middle(float up)
        {
            float at = Mathf.Clamp((up - FootTop) / (Start - FootTop), 0f, 1f) * (middles.Length - 1);
            int i = Mathf.Min((int)at, middles.Length - 2);
            return Mathf.Lerp(middles[i], middles[i + 1], at - i);
        }

        // The neck's middle in bands from the foot's top to head_start: the mean z of the corners at its sides (the widest
        // across in the band), so a mane or a ridge down its back does not pull it; a band with none takes its neighbour's.
        private static float[] Middles(Vector3[] vertices, float start)
        {
            int count = Mathf.Max(2, Mathf.CeilToInt((start - FootTop) / Band) + 1);
            float[] widest = new float[count], sum = new float[count];
            int[] found = new int[count];
            float step = (start - FootTop) / (count - 1);
            Func<Vector3, int> band = v => Mathf.RoundToInt((v.y - FootTop) / step);
            foreach (Vector3 v in vertices)
                if (band(v) >= 0 && band(v) < count)
                    widest[band(v)] = Mathf.Max(widest[band(v)], Mathf.Abs(v.x));
            foreach (Vector3 v in vertices)
            {
                int b = band(v);
                if (b >= 0 && b < count && Mathf.Abs(v.x) >= Side * widest[b])
                    (sum[b], found[b]) = (sum[b] + v.z, found[b] + 1);
            }
            return Filled(sum, found);
        }

        // Each band's mean, a band with no corners taking the one below it (the foot's middle, 0, under the first).
        private static float[] Filled(float[] sum, int[] found)
        {
            float[] middles = new float[sum.Length];
            for (int i = 0; i < sum.Length; i++)
                middles[i] = found[i] > 0 ? sum[i] / found[i] : i > 0 ? middles[i - 1] : 0f;
            return middles;
        }

        // The corners moved so the middle of the foot's underside is the origin across and along: the bundle's prefab roots
        // stand at the foot, but its meshes were left where the workshop laid the five heads out side by side (2026-10-25).
        private static Vector3[] Centred(Vector3[] vertices)
        {
            Vector2 low = new Vector2(float.MaxValue, float.MaxValue), high = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 v in vertices)
            {
                if (v.y > Underside)
                    continue;
                low = Vector2.Min(low, new Vector2(v.x, v.z));
                high = Vector2.Max(high, new Vector2(v.x, v.z));
            }
            if (low.x > high.x)
                return vertices;
            Vector3 middle = new Vector3((low.x + high.x) / 2f, 0f, (low.y + high.y) / 2f);
            Vector3[] centred = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
                centred[i] = vertices[i] - middle;
            return centred;
        }
    }
}
