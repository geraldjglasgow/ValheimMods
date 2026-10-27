using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// Meshes cut for a damaged rig piece's look (<see cref="KwRigLook"/>): the triangles a test keeps, copied with their
    /// points' normals, tangents, UVs and colours (<see cref="Kept"/>); a mesh's points pushed into craters
    /// (<see cref="Chipped"/>); meshes joined (<see cref="Joined"/>). Points are tested in the piece's kit axes.
    /// </summary>
    public static class KwRigDamageMesh
    {
        /// <summary>A mesh's triangles a test keeps (given each triangle's three points in the piece's kit axes).</summary>
        public static Mesh Kept(Mesh whole, Vector3[] kit, Func<Vector3, Vector3, Vector3, bool> keep, string name) =>
            Compact(whole, Triangles(whole.triangles, kit, keep), name);

        /// <summary>The triangles (three points' indices each) a test keeps, given their points in the piece's kit axes.</summary>
        public static List<int> Triangles(int[] triangles, Vector3[] kit, Func<Vector3, Vector3, Vector3, bool> keep)
        {
            List<int> kept = new List<int>(triangles.Length);
            for (int k = 0; k + 2 < triangles.Length; k += 3)
            {
                if (keep(kit[triangles[k]], kit[triangles[k + 1]], kit[triangles[k + 2]]))
                    kept.AddRange(new[] { triangles[k], triangles[k + 1], triangles[k + 2] });
            }
            return kept;
        }

        // A new mesh of some of a mesh's triangles, its points those they use, with their normals, tangents, UVs and colours.
        private static Mesh Compact(Mesh whole, List<int> triangles, string name)
        {
            Dictionary<int, int> map = new Dictionary<int, int>();
            List<int> order = new List<int>();
            for (int k = 0; k < triangles.Count; k++)
            {
                if (!map.TryGetValue(triangles[k], out int at))
                {
                    map[triangles[k]] = at = order.Count;
                    order.Add(triangles[k]);
                }
                triangles[k] = at;
            }
            Mesh made = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            Copy(whole, made, order);
            made.SetTriangles(triangles, 0);
            made.RecalculateBounds();
            return made;
        }

        // The channels the source has, for the points kept, in order.
        private static void Copy(Mesh whole, Mesh made, List<int> order)
        {
            Vector3[] v = whole.vertices, n = whole.normals;
            Vector4[] t = whole.tangents;
            Vector2[] uv = whole.uv;
            Color[] colours = whole.colors;
            made.SetVertices(order.ConvertAll(i => v[i]));
            if (n.Length == v.Length)
                made.SetNormals(order.ConvertAll(i => n[i]));
            if (t.Length == v.Length)
                made.SetTangents(order.ConvertAll(i => t[i]));
            if (uv.Length == v.Length)
                made.SetUVs(0, order.ConvertAll(i => uv[i]));
            if (colours.Length == v.Length)
                made.SetColors(order.ConvertAll(i => colours[i]));
        }

        /// <summary>
        /// A mesh with craters where chips came off (<see cref="KwRigMark.Chip"/>, in the piece's kit axes): each point
        /// inside a chip's ball pushed in along its normal to the ball's far side, so a bite is out of the wood.
        /// </summary>
        public static void Chipped(Mesh mesh, Matrix4x4 kitToMesh, List<KwRigMark> chips)
        {
            Vector3[] v = mesh.vertices, n = mesh.normals;
            if (n.Length != v.Length || chips.Count == 0)
                return;
            Matrix4x4 toKit = kitToMesh.inverse;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = KwMesh.ToKit(toKit.MultiplyPoint3x4(v[i])), normal = KwMesh.ToKit(toKit.MultiplyVector(n[i])).normalized;
                foreach (KwRigMark chip in chips)
                    p = Crater(p, normal, new Vector3(chip.A, chip.B, chip.C), chip.D);
                v[i] = kitToMesh.MultiplyPoint3x4(KwMesh.ToUnity(p));
            }
            mesh.SetVertices(v);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>A point inside a ball moved in along its normal to the ball's far side; one outside it as it is.</summary>
        public static Vector3 Crater(Vector3 p, Vector3 normal, Vector3 middle, float radius)
        {
            Vector3 w = p - middle;
            if (w.sqrMagnitude >= radius * radius)
                return p;
            float along = Vector3.Dot(w, normal);
            float t = along + Mathf.Sqrt(Mathf.Max(0f, along * along - w.sqrMagnitude + radius * radius));
            return p - normal * t;
        }

        /// <summary>Meshes joined into one, each placed by its own matrix; the parts are let go.</summary>
        public static Mesh Joined(List<(Mesh Mesh, Matrix4x4 At)> parts, string name)
        {
            Mesh made = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            List<CombineInstance> join = parts.FindAll(p => p.Mesh != null && p.Mesh.vertexCount > 0).ConvertAll(p => new CombineInstance { mesh = p.Mesh, transform = p.At });
            if (join.Count > 0)
                made.CombineMeshes(join.ToArray(), true, true);
            made.RecalculateBounds();
            foreach ((Mesh mesh, Matrix4x4 _) in parts)
            {
                if (mesh != null)
                    UnityEngine.Object.Destroy(mesh);
            }
            return made;
        }
    }
}
