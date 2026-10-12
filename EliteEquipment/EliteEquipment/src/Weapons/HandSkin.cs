using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Weapons
{
    /// <summary>
    /// Builds a weapon mesh skinned to the player's hands, as the game's dual axes and fist weapons are (one
    /// SkinnedMeshRenderer under <c>attach_skin</c> on the body's 53 bones; the game gives it the body's bones when worn).
    /// Each vertex goes to one hand bone with weight 1, moved into that bone's bind space through the hand's attach point:
    /// <c>inverse(bindposes[bone]) * handAttach * v</c>, so the game's own bind poses stay. The bundle meshes must be
    /// readable (the workshop imports these with Read/Write on).
    /// </summary>
    public sealed class HandSkin
    {
        public const int RightHand = 29;
        public const int LeftHand = 10;

        /// <summary>The player's RightHand_Attach and LeftHand_Attach in their hand bones (read from the player prefab).</summary>
        public static readonly Matrix4x4 RightAttach = Matrix4x4.TRS(new Vector3(0f, 0.00105f, 0.0003f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f), Vector3.one * 0.01f);
        public static readonly Matrix4x4 LeftAttach = Matrix4x4.TRS(new Vector3(0f, 0.00105f, 0.0003f), new Quaternion(0.5f, 0.5f, 0.5f, 0.5f), Vector3.one * 0.01f);

        private readonly Matrix4x4[] bindposes;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<BoneWeight> weights = new List<BoneWeight>();

        public HandSkin(Mesh game)
        {
            bindposes = game.bindposes;
            if (bindposes == null || bindposes.Length <= RightHand)
                throw new InvalidOperationException($"{game.name} has no hand bind poses");
        }

        /// <summary>The matrix that takes a point in a hand's attach frame into that hand bone's bind space.</summary>
        public Matrix4x4 Hand(int bone, Matrix4x4 attach) => bindposes[bone].inverse * attach;

        /// <summary>Adds a mesh; <paramref name="place"/> gives each vertex (mesh space) its bone and its matrix into bind space.</summary>
        public void Add(Mesh mesh, Func<Vector3, (int bone, Matrix4x4 matrix)> place)
        {
            int start = vertices.Count;
            Vector3[] points = mesh.vertices;
            Vector3[] normal = mesh.normals;
            Vector2[] uv = mesh.uv;
            for (int i = 0; i < points.Length; i++)
            {
                (int bone, Matrix4x4 matrix) = place(points[i]);
                vertices.Add(matrix.MultiplyPoint3x4(points[i]));
                normals.Add(i < normal.Length ? matrix.MultiplyVector(normal[i]).normalized : Vector3.up);
                uvs.Add(i < uv.Length ? uv[i] : Vector2.zero);
                weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1f });
            }
            foreach (int index in mesh.triangles)
                triangles.Add(start + index);
        }

        public Mesh Finish(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.boneWeights = weights.ToArray();
            mesh.bindposes = bindposes;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
