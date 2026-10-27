using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A sail's cloth as points: its mesh is built face by face (every face its own corners, <see cref="MeshBuilder"/>),
    /// so the corners where faces meet are moved as one point and lit as one, with a normal from all the faces round
    /// it, and the cloth is shaded smooth as the game's sails are, not face by face (each face catching the light on its
    /// own as the cloth moves glitters). Corners within a millimetre are one point.
    /// </summary>
    public sealed class KwSailWeld
    {
        private readonly int[] of, triangles;
        private readonly Vector3[] normals;

        /// <summary>The points of a mesh's vertices (as they sit slack) and its triangles.</summary>
        public KwSailWeld(Vector3[] vertices, int[] meshTriangles)
        {
            Dictionary<Vector3Int, int> seen = new Dictionary<Vector3Int, int>();
            List<int> firsts = new List<int>();
            of = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3Int key = Vector3Int.RoundToInt(vertices[i] * 1000f);
                if (!seen.TryGetValue(key, out int point))
                {
                    seen[key] = point = firsts.Count;
                    firsts.Add(i);
                }
                of[i] = point;
            }
            Firsts = firsts.ToArray();
            triangles = new int[meshTriangles.Length];
            for (int t = 0; t < triangles.Length; t++)
                triangles[t] = of[meshTriangles[t]];
            normals = new Vector3[Firsts.Length];
        }

        /// <summary>Each point's first vertex.</summary>
        public int[] Firsts { get; }

        /// <summary>Each point's value from an array of the vertices'.</summary>
        public Vector3[] Pick(Vector3[] vertices)
        {
            Vector3[] picked = new Vector3[Firsts.Length];
            for (int k = 0; k < picked.Length; k++)
                picked[k] = vertices[Firsts[k]];
            return picked;
        }

        /// <summary>The normals at the points where they are now: the faces round each, the larger counting more; fallback where none has any size.</summary>
        public Vector3[] Normals(Vector3[] points, Vector3 fallback)
        {
            System.Array.Clear(normals, 0, normals.Length);
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                Vector3 n = Vector3.Cross(points[b] - points[a], points[c] - points[a]);
                normals[a] += n;
                normals[b] += n;
                normals[c] += n;
            }
            for (int k = 0; k < normals.Length; k++)
                normals[k] = normals[k].sqrMagnitude > 1e-12f ? normals[k].normalized : fallback;
            return normals;
        }

        /// <summary>Every vertex given its point's value.</summary>
        public void Spread(Vector3[] points, Vector3[] into)
        {
            for (int i = 0; i < of.Length; i++)
                into[i] = points[of[i]];
        }
    }
}
