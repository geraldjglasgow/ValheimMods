using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// A chest's hanging cloth (the Protector's tabards; <see cref="ChestShape"/>) over fitted leggings. The front one
    /// hangs down from the belt (each vertex takes the depth the cloth has at the belt line, by its place across) but
    /// never through what lies behind it (the body, the leggings and the plates, with room to spare), shortened to end at
    /// the shape's height (squeezed up toward the waist, so it keeps its shape and ornament), and weighted like the belt
    /// above it, so it no longer stretches between the legs; then it leaves the chest for a small mesh of its own that the
    /// game's cloth simulation can move (<see cref="ChestClothSim"/>): its second texture channel marks the belt row fixed
    /// and the rest free. The back one is left out (the user, 2026-10-09: "remove the back blue dangling cloth").
    /// </summary>
    internal static class ChestCloth
    {
        /// <summary>Above this the cloth is the belt's and stays with the chest's other blue cloth out (the hood, the collar).</summary>
        private const float Top = 1.2f;
        private const float BeltRow = 0.05f;
        private const float Fixed = 0.03f;
        private const float Room = 0.015f;
        private const float Near = 0.05f;

        /// <summary>The front cloth hung straight, clear of what is behind it, and shortened to end at <paramref name="bottom"/>.</summary>
        public static void Hang(ChestVertices v, float bottom)
        {
            List<int> cloth = Front(v);
            List<int> belt = cloth.FindAll(i => v.Rest[i].y > ChestVertices.From - BeltRow);
            if (cloth.Count == 0 || belt.Count == 0)
                return;
            float lowest = float.MaxValue;
            foreach (int i in cloth)
                lowest = Mathf.Min(lowest, v.Rest[i].y);
            float squeeze = lowest < bottom ? (ChestVertices.From - bottom) / (ChestVertices.From - lowest) : 1f;
            foreach (int i in cloth)
            {
                int above = Nearest(v, belt, v.Rest[i].x);
                var hung = new Vector3(v.Rest[i].x, ChestVertices.From - (ChestVertices.From - v.Rest[i].y) * squeeze, v.Rest[above].z);
                v.Rest[i] = new Vector3(hung.x, hung.y, Mathf.Max(hung.z, Clear(v, hung)));
                v.Weights[i] = v.Weights[above];
            }
        }

        /// <summary>How far forward the cloth must lie at <paramref name="at"/> to stay off the body and plates.</summary>
        private static float Clear(ChestVertices v, Vector3 at)
        {
            float bound = v.Envelope.Axis.y + v.Envelope.At(at) + v.Thickness + Room;
            for (int j = 0; j < v.Rest.Length; j++)
            {
                Vector3 r = v.Rest[j];
                if (v.Kind[j] != ChestPart.Plate || Mathf.Abs(r.x - at.x) > Near || Mathf.Abs(r.y - at.y) > Near)
                    continue;
                bound = Mathf.Max(bound, r.z + Room);
            }
            return bound;
        }

        /// <summary>
        /// The front cloth's triangles taken out of <paramref name="chest"/> into a mesh of their own, the back cloth's
        /// dropped; null, and nothing taken, when the chest has no front cloth.
        /// </summary>
        public static Mesh Split(Mesh chest, ChestVertices v, ChestShape shape)
        {
            int[] triangles = chest.GetTriangles(0);
            var kept = new List<int>(triangles.Length);
            var front = new List<int>();
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                bool hangs = IsCloth(v, shape, a) && IsCloth(v, shape, b) && IsCloth(v, shape, c);
                if (!hangs)
                    kept.AddRange(new[] { a, b, c });
                else if (v.Rest[a].z + v.Rest[b].z + v.Rest[c].z > 3f * v.Envelope.Axis.y)
                    front.AddRange(new[] { a, b, c });
            }
            if (front.Count == 0)
                return null;
            Mesh own = Compact(chest, v, front);
            chest.SetTriangles(kept, 0);
            return own;
        }

        private static bool IsCloth(ChestVertices v, ChestShape shape, int i) =>
            v.Rest[i].y < Top && v.Kind[i] != ChestPart.Arm && shape.IsCloth(v.Uv[i]);

        /// <summary>A mesh of just these triangles and their vertices, in the chest's bind poses; readable, as the simulation needs.</summary>
        private static Mesh Compact(Mesh chest, ChestVertices v, List<int> triangles)
        {
            var index = new Dictionary<int, int>();
            var used = new List<int>();
            var remapped = new List<int>(triangles.Count);
            foreach (int i in triangles)
            {
                if (!index.TryGetValue(i, out int n))
                {
                    index[i] = n = used.Count;
                    used.Add(i);
                }
                remapped.Add(n);
            }
            var mesh = new Mesh { name = chest.name.Replace("_ee_fitted", "_ee_cloth") };
            Fill(mesh, chest, v, used);
            mesh.SetTriangles(remapped, 0);
            mesh.bindposes = chest.bindposes;
            mesh.bounds = chest.bounds;
            return mesh;
        }

        private static void Fill(Mesh mesh, Mesh chest, ChestVertices v, List<int> used)
        {
            Vector3[] vertices = chest.vertices, normals = chest.normals;
            Vector4[] tangents = chest.tangents;
            Vector2[] uv = chest.uv;
            BoneWeight[] weights = chest.boneWeights;
            mesh.SetVertices(used.ConvertAll(i => vertices[i]));
            mesh.SetNormals(used.ConvertAll(i => normals[i]));
            if (tangents.Length == vertices.Length)
                mesh.SetTangents(used.ConvertAll(i => tangents[i]));
            mesh.SetUVs(0, used.ConvertAll(i => uv[i]));
            mesh.SetUVs(1, used.ConvertAll(i => new Vector2(v.Rest[i].y >= ChestVertices.From - Fixed ? 0.25f : 0.75f, 0.5f)));
            mesh.boneWeights = used.ConvertAll(i => weights[i]).ToArray();
        }

        private static List<int> Front(ChestVertices v)
        {
            var front = new List<int>();
            for (int i = 0; i < v.Rest.Length; i++)
            {
                if (v.Kind[i] == ChestPart.Cloth && v.Rest[i].z > v.Envelope.Axis.y)
                    front.Add(i);
            }
            return front;
        }

        private static int Nearest(ChestVertices v, List<int> among, float x)
        {
            int best = among[0];
            foreach (int j in among)
            {
                if (Mathf.Abs(v.Rest[j].x - x) < Mathf.Abs(v.Rest[best].x - x))
                    best = j;
            }
            return best;
        }
    }
}
