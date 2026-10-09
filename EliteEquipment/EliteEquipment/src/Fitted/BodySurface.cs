using System;
using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// A player body mesh as fitted leggings are made from it: its vertices, skin weights, UVs and first submesh's triangles,
    /// each vertex's rest position in metres (the bind pose, where the mesh's z is up), one outward normal per place
    /// (the vertices doubled along texture seams share it, so leggings pushed out along it have no cracks) and which vertices
    /// the arms, hands or head move. Null for a mesh that is not readable or not a player's size (another mod's body).
    /// </summary>
    internal sealed class BodySurface
    {
        /// <summary>Metres per mesh unit in the player's bind pose (the game's armature is scaled 100, the model 0.95).</summary>
        public const float Scale = 95f;

        private static readonly string[] UpperBones = { "Arm", "Hand", "Shoulder", "Neck", "Head", "Jaw", "Thumb", "Index", "Middle", "Ring", "Pinky" };

        private BodySurface(Mesh mesh, string[] bones)
        {
            Mesh = mesh;
            Vertices = mesh.vertices;
            Weights = mesh.boneWeights;
            Uvs = mesh.uv;
            Triangles = mesh.GetTriangles(0);
            Rest = Array.ConvertAll(Vertices, ToRest);
            Outward = Welded(mesh.normals);
            Upper = UpperOf(Weights, bones);
        }

        public Mesh Mesh { get; }
        public Vector3[] Vertices { get; }
        public BoneWeight[] Weights { get; }
        public Vector2[] Uvs { get; }
        public int[] Triangles { get; }

        /// <summary>Each vertex's bind pose position in metres, Unity's axes (x to the right, y up, z forward).</summary>
        public Vector3[] Rest { get; }

        /// <summary>Each vertex's outward normal in mesh space, the same for every vertex at one place.</summary>
        public Vector3[] Outward { get; }

        /// <summary>The vertices the arms, hands or head move: never under the leggings.</summary>
        public bool[] Upper { get; }

        public static BodySurface Of(Mesh mesh, string[] bones)
        {
            if (mesh == null || !mesh.isReadable || mesh.vertexCount == 0 || mesh.boneWeights.Length != mesh.vertexCount)
                return null;
            float height = mesh.bounds.size.z * Scale;
            return height > 1.4f && height < 2.4f ? new BodySurface(mesh, bones) : null;
        }

        public static Vector3 ToRest(Vector3 p) => new Vector3(Scale * p.x, Scale * p.z, -Scale * p.y);

        /// <summary>A rest position (metres) back in mesh space.</summary>
        public static Vector3 FromRest(Vector3 r) => new Vector3(r.x / Scale, -r.z / Scale, r.y / Scale);

        /// <summary>A vertex moved <paramref name="metres"/> out along its outward normal, in mesh space.</summary>
        public Vector3 Out(int vertex, float metres) => Vertices[vertex] + Outward[vertex] * (metres / Scale);

        /// <summary>The vertices' place to half a millimetre, the same for the copies along a texture seam.</summary>
        public Vector3Int Place(int vertex) => Vector3Int.RoundToInt(Rest[vertex] * 2000f);

        private Vector3[] Welded(Vector3[] normals)
        {
            var sums = new Dictionary<Vector3Int, Vector3>();
            for (int i = 0; i < normals.Length; i++)
                sums[Place(i)] = (sums.TryGetValue(Place(i), out Vector3 sum) ? sum : Vector3.zero) + normals[i];
            var outward = new Vector3[normals.Length];
            for (int i = 0; i < normals.Length; i++)
                outward[i] = sums[Place(i)].normalized;
            return outward;
        }

        /// <summary>Which of a skinned mesh's vertices (weights indexing <paramref name="bones"/> by name) the arms, hands or head move.</summary>
        public static bool[] UpperOf(BoneWeight[] weights, string[] bones)
        {
            var upperBone = Array.ConvertAll(bones, name => Array.Exists(UpperBones, part => name != null && name.Contains(part)));
            var upper = new bool[weights.Length];
            for (int i = 0; i < upper.Length; i++)
            {
                BoneWeight w = weights[i];
                upper[i] = On(upperBone, w.boneIndex0, w.weight0) + On(upperBone, w.boneIndex1, w.weight1)
                    + On(upperBone, w.boneIndex2, w.weight2) + On(upperBone, w.boneIndex3, w.weight3) >= 0.02f;
            }
            return upper;
        }

        private static float On(bool[] upperBone, int bone, float weight) => bone >= 0 && bone < upperBone.Length && upperBone[bone] ? weight : 0f;
    }
}
