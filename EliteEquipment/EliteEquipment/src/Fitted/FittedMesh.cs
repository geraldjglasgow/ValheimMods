using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>The fitted leggings' vertices (mesh space, the body's), texture coordinates, skin weights and triangles as they are made.</summary>
    internal sealed class FittedMesh
    {
        private readonly List<Vector3> positions = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<BoneWeight> weights = new List<BoneWeight>();
        private readonly List<int> triangles = new List<int>();

        public int TriangleCount => triangles.Count / 3;

        public int Add(Vector3 position, Vector3 normal, Vector2 uv, BoneWeight weight)
        {
            positions.Add(position);
            normals.Add(normal);
            uvs.Add(uv);
            weights.Add(weight);
            return positions.Count - 1;
        }

        public void Triangle(int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        /// <summary>A triangle drawn from both sides.</summary>
        public void Both(int a, int b, int c)
        {
            Triangle(a, b, c);
            Triangle(a, c, b);
        }

        /// <summary>A convex polygon of consecutive vertices from <paramref name="first"/>, as a fan.</summary>
        public void Fan(int first, int count)
        {
            for (int i = 1; i + 1 < count; i++)
                Triangle(first, first + i, first + i + 1);
        }

        public Mesh ToMesh(string name, Matrix4x4[] bindposes)
        {
            var mesh = new Mesh { name = name };
            if (positions.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.boneWeights = weights.ToArray();
            mesh.bindposes = bindposes;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
