using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A generated patch's boards (<see cref="KwHullPatchBoard"/>, while the mod embeds no workshop patch): boards up the
    /// face (its y), each running along the planks (its x) across the whole patch, <see cref="Thick"/> thick, their backs on
    /// z = 0 and their fronts out of the hull (+z), lapped <see cref="Lap"/> over each other as clinker strakes are (every
    /// other board <see cref="Proud"/> further out), so no crack opens over the hole; each a little longer or shorter, shifted
    /// and turned (its ends never more than <see cref="Skew"/> up or down), from the hole's own number, so no two patches lie
    /// alike and every peer draws the same. Every face its own normal, wound to face out; UVs along each board's grain, a
    /// metre to half the plank wood's texture. Plain lists (<see cref="Lay"/>) but the mesh itself.
    /// </summary>
    public static class KwHullPatchMesh
    {
        public const float Thick = 0.03f, Board = 0.16f, Lap = 0.03f, Proud = 0.004f, Skew = 0.012f, Shift = 0.05f;
        private static readonly Vector3[] Faces = { Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.right, Vector3.left };

        /// <summary>The boards' mesh for a patch reaching a distance from its middle every way across.</summary>
        public static Mesh Boards(float reach, int seed)
        {
            KwPatchLists lay = Lay(reach, seed);
            Mesh mesh = new Mesh { name = "KW_hull_patch" };
            mesh.SetVertices(lay.Points);
            mesh.SetNormals(lay.Normals);
            mesh.SetUVs(0, lay.Uvs);
            mesh.SetTriangles(lay.Triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>The boards as lists: 24 corners a board, 6 faces of two triangles.</summary>
        public static KwPatchLists Lay(float reach, int seed)
        {
            KwPatchLists lay = new KwPatchLists();
            System.Random random = new System.Random(seed);
            float high = 2f * reach;
            int n = Mathf.Clamp(Mathf.RoundToInt(high / Board), 2, 6);
            float wide = (high + Lap * (n - 1)) / n;
            for (int i = 0; i < n; i++)
            {
                float length = high * (0.94f + 0.12f * (float)random.NextDouble()), grain = (float)random.NextDouble();
                Vector3 middle = new Vector3(Shift * ((float)random.NextDouble() - 0.5f), -reach + wide / 2f + i * (wide - Lap), Thick / 2f + Proud * (i % 2));
                float half = Mathf.Atan2(Skew, length / 2f) * (2f * (float)random.NextDouble() - 1f) / 2f;
                Box(lay, middle, new Vector3(length, wide, Thick), new Quaternion(0f, 0f, Mathf.Sin(half), Mathf.Cos(half)), grain);
            }
            return lay;
        }

        // One board: a box of a size about its middle, turned about z, each face its four corners (wound so the face looks out).
        private static void Box(KwPatchLists lay, Vector3 middle, Vector3 size, Quaternion turn, float grain)
        {
            foreach (Vector3 n in Faces)
            {
                Vector3 u = Mathf.Abs(n.x) > 0.5f ? Vector3.forward : Vector3.right, w = Vector3.Cross(n, u);
                int start = lay.Points.Count;
                for (int k = 0; k < 4; k++)
                {
                    Vector3 local = Vector3.Scale(n + u * (k == 1 || k == 2 ? 1f : -1f) + w * (k >= 2 ? 1f : -1f), size / 2f);
                    lay.Points.Add(middle + turn * local);
                    lay.Normals.Add(turn * n);
                    lay.Uvs.Add(new Vector2(grain + (local.x + size.x / 2f) / 2f, (local.y + local.z) / 2f + 0.5f));
                }
                lay.Triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
        }
    }

    /// <summary>A mesh as plain lists (<see cref="KwHullPatchMesh.Lay"/>).</summary>
    public sealed class KwPatchLists
    {
        public List<Vector3> Points { get; } = new List<Vector3>();
        public List<Vector3> Normals { get; } = new List<Vector3>();
        public List<Vector2> Uvs { get; } = new List<Vector2>();
        public List<int> Triangles { get; } = new List<int>();
    }
}
