using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A seer's robe split in two for its cloth (<see cref="RobeCloth"/>), once per robe mesh: the skirt's own mesh (only the
    /// triangles with a moving vertex, and their vertices: the skirt and the fixed band it hangs from) drawn by a renderer of
    /// its own beside the robe's, which the cloth moves, and the rest of the robe (every other triangle) on the robe's own
    /// renderer. MagicaCloth takes a renderer's vertices for its cloth by place, not by index: every vertex within about 8 cm
    /// of a listed one joins the cloth (in game 1747 of the Plains robe's 2461 vertices for 1377 listed), and its reduction
    /// merges vertices a few centimetres apart whatever part they belong to, adding their bone weights together. On the whole
    /// robe a sleeve's cuff beside the skirt when the cloth was built was merged into it and the skirt hung from the hand
    /// (user, 2026-11-02: "the bottom robe is like somehow attached to the top robe"). On a mesh of its own only the skirt can.
    /// The two overlap by a row (<see cref="Overlap"/>): the cloth moves even its held vertices a little (its reduction merges
    /// them with their neighbours, adding their bone weights, and turns them with the cloth below), so where the two meshes
    /// met edge to edge they parted into a dark crack round the skirt's top (user, 2026-11-03: "random horizontal cuts").
    /// The skirt's mesh also carries the hidden ties of the layers over it (<see cref="Tie"/>).
    /// </summary>
    internal sealed class RobeClothMesh
    {
        /// <summary>The skirt's own mesh, the robe without it, each skirt vertex's part in the cloth, the skirt's longest side in metres.</summary>
        public Mesh Skirt, Rest;
        public VertexAttribute[] Attributes;
        public float Side;

        /// <summary>The skirt's own renderer's name (a child of the robe's).</summary>
        public const string SkirtName = "EE_RobeSkirt";

        // Metres the skirt's copy of the overlapping row is pulled in behind the robe's own (always behind it, never level).
        private const float Behind = 0.002f;

        private static readonly Dictionary<Mesh, RobeClothMesh> made = new Dictionary<Mesh, RobeClothMesh>();

        public static RobeClothMesh Of(Mesh source, VertexAttribute[] attributes)
        {
            if (!made.TryGetValue(source, out RobeClothMesh parts))
                made[source] = parts = Split(source, attributes);
            return parts;
        }

        private static RobeClothMesh Split(Mesh source, VertexAttribute[] attributes)
        {
            int count = source.subMeshCount;
            var skirt = new List<int>[count];
            var rest = new List<int>[count];
            for (int s = 0; s < count; s++)
                Sort(source.GetTriangles(s), attributes, skirt[s] = new List<int>(), rest[s] = new List<int>());
            bool[] inward = Overlap(skirt, rest, attributes, source.vertexCount);
            var parts = new RobeClothMesh { Rest = Object.Instantiate(source) };
            parts.Rest.name = source.name + " rest";
            for (int s = 0; s < count; s++)
                parts.Rest.SetTriangles(rest[s], s);
            parts.Skirt = Copy(source, skirt, attributes, inward, out parts.Attributes);
            Vector3 size = parts.Skirt.bounds.size;
            parts.Side = Mathf.Max(size.x, Mathf.Max(size.y, size.z)) * RobeClothFixed.Scale;
            return parts;
        }

        /// <summary>A triangle with a moving vertex goes to the skirt, any other to the rest of the robe.</summary>
        private static void Sort(int[] triangles, VertexAttribute[] attributes, List<int> skirt, List<int> rest)
        {
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                bool moves = attributes[triangles[t]].IsMove() || attributes[triangles[t + 1]].IsMove()
                    || attributes[triangles[t + 2]].IsMove();
                List<int> to = moves ? skirt : rest;
                to.Add(triangles[t]);
                to.Add(triangles[t + 1]);
                to.Add(triangles[t + 2]);
            }
        }

        /// <summary>
        /// The robe's row of held triangles along the skirt's top edge (all three vertices held, one on the edge, the edge
        /// being the vertices both meshes draw) goes to the skirt as well, so the robe's band covers the join in front and the
        /// skirt's copy closes any gap behind it. Returns the vertices the skirt gains, which its copy pulls in behind.
        /// </summary>
        private static bool[] Overlap(List<int>[] skirt, List<int>[] rest, VertexAttribute[] attributes, int vertices)
        {
            bool[] inSkirt = Used(skirt, vertices), edge = new bool[vertices], inward = new bool[vertices];
            foreach (List<int> list in rest)
            {
                foreach (int vertex in list)
                    edge[vertex] |= inSkirt[vertex];
            }
            for (int s = 0; s < rest.Length; s++)
            {
                for (int t = 0; t + 2 < rest[s].Count; t += 3)
                {
                    if (Joins(rest[s], t, edge, attributes))
                        Share(rest[s], t, skirt[s], inSkirt, inward);
                }
            }
            return inward;
        }

        private static bool Joins(List<int> triangles, int t, bool[] edge, VertexAttribute[] attributes)
        {
            int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
            return (edge[a] || edge[b] || edge[c]) && attributes[a].IsFixed() && attributes[b].IsFixed() && attributes[c].IsFixed();
        }

        private static void Share(List<int> triangles, int t, List<int> skirt, bool[] inSkirt, bool[] inward)
        {
            for (int k = t; k < t + 3; k++)
            {
                skirt.Add(triangles[k]);
                inward[triangles[k]] |= !inSkirt[triangles[k]];
            }
        }

        /// <summary>The skirt's vertex positions: the overlapping row's own vertices pulled in along their normals.</summary>
        private static Vector3[] Positions(Mesh source, bool[] inward)
        {
            Vector3[] positions = source.vertices, normals = source.normals;
            for (int i = 0; i < positions.Length && i < normals.Length; i++)
            {
                if (inward[i])
                    positions[i] -= normals[i] * (Behind / RobeClothFixed.Scale);
            }
            return positions;
        }

        /// <summary>A mesh of only the vertices these triangles use, in the source's order, on the same bones.</summary>
        private static Mesh Copy(Mesh source, List<int>[] triangles, VertexAttribute[] attributes, bool[] inward, out VertexAttribute[] kept)
        {
            int[] remap = Remap(source.vertexCount, triangles, out int count);
            var mesh = new Mesh { name = source.name + " skirt", indexFormat = source.indexFormat };
            mesh.vertices = Pick(Positions(source, inward), remap, count);
            mesh.normals = Pick(source.normals, remap, count);
            mesh.tangents = Pick(source.tangents, remap, count);
            mesh.uv = Pick(source.uv, remap, count);
            Color[] colors = source.colors;
            if (colors.Length > 0)
                mesh.colors = Pick(colors, remap, count);
            mesh.boneWeights = Pick(source.boneWeights, remap, count);
            mesh.bindposes = source.bindposes;
            mesh.subMeshCount = triangles.Length;
            for (int s = 0; s < triangles.Length; s++)
                mesh.SetTriangles(Renumber(triangles[s], remap), s);
            kept = Pick(attributes, remap, count);
            Tie(mesh, kept);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The layers hung over the skirt tied to the cloth under them (<see cref="RobeClothTies"/>): the tie triangles go in a
        /// submesh after the drawn ones, which MagicaCloth reads with the rest and the renderer draws with no material.
        /// </summary>
        private static void Tie(Mesh mesh, VertexAttribute[] kept)
        {
            int[] ties = RobeClothTies.Of(mesh.vertices, mesh.normals, mesh.triangles, kept);
            if (ties.Length == 0)
                return;
            int drawn = mesh.subMeshCount;
            mesh.subMeshCount = drawn + 1;
            mesh.SetTriangles(ties, drawn);
            Plugin.Log.LogInfo($"EliteEquipment: {mesh.name}: {ties.Length / 9} vertices of the layers over the skirt tied to it");
        }

        private static bool[] Used(List<int>[] triangles, int vertices)
        {
            var used = new bool[vertices];
            foreach (List<int> list in triangles)
            {
                foreach (int vertex in list)
                    used[vertex] = true;
            }
            return used;
        }

        private static int[] Remap(int vertices, List<int>[] triangles, out int count)
        {
            var remap = new int[vertices];
            bool[] used = Used(triangles, vertices);
            count = 0;
            for (int i = 0; i < vertices; i++)
                remap[i] = used[i] ? count++ : -1;
            return remap;
        }

        /// <summary>The kept vertices' values in their new order; an empty array for a channel the source does not have.</summary>
        private static T[] Pick<T>(T[] from, int[] remap, int count)
        {
            if (from == null || from.Length != remap.Length)
                return new T[0];
            var picked = new T[count];
            for (int i = 0; i < remap.Length; i++)
            {
                if (remap[i] >= 0)
                    picked[remap[i]] = from[i];
            }
            return picked;
        }

        private static int[] Renumber(List<int> triangles, int[] remap)
        {
            var renumbered = new int[triangles.Count];
            for (int i = 0; i < renumbered.Length; i++)
                renumbered[i] = remap[triangles[i]];
            return renumbered;
        }

        /// <summary>
        /// The skirt drawn by its own renderer under the robe's (its bones, materials, property block and shadows) and the rest
        /// of the robe by the robe's; the caller adds it to the wearer's level of detail.
        /// </summary>
        public SkinnedMeshRenderer Wear(SkinnedMeshRenderer robe)
        {
            var holder = new GameObject(SkirtName) { layer = robe.gameObject.layer };
            holder.transform.SetParent(robe.transform, false);
            SkinnedMeshRenderer skirt = holder.AddComponent<SkinnedMeshRenderer>();
            skirt.bones = robe.bones;
            skirt.rootBone = robe.rootBone;
            skirt.sharedMesh = Skirt;
            skirt.sharedMaterials = Drawn(robe.sharedMaterials);
            skirt.localBounds = robe.localBounds;
            skirt.updateWhenOffscreen = robe.updateWhenOffscreen;
            skirt.shadowCastingMode = robe.shadowCastingMode;
            skirt.receiveShadows = robe.receiveShadows;
            skirt.quality = robe.quality;
            var block = new MaterialPropertyBlock();
            robe.GetPropertyBlock(block);
            skirt.SetPropertyBlock(block);
            robe.sharedMesh = Rest;
            return skirt;
        }

        /// <summary>The robe's materials for the skirt's drawn submeshes only: one more would draw the hidden tie submesh.</summary>
        private Material[] Drawn(Material[] materials)
        {
            int drawn = Rest.subMeshCount;
            if (materials.Length <= drawn)
                return materials;
            var kept = new Material[drawn];
            System.Array.Copy(materials, kept, drawn);
            return kept;
        }

        /// <summary>The robe's whole mesh back on its renderer and the skirt's renderer gone, out of the wearer's level of detail.</summary>
        public static void TakeOff(SkinnedMeshRenderer robe, Mesh source, SkinnedMeshRenderer skirt, VisEquipment vis)
        {
            if (skirt != null)
            {
                skirt.gameObject.SetActive(false);
                Object.Destroy(skirt.gameObject);
            }
            if (robe != null && robe.sharedMesh != source)
                robe.sharedMesh = source;
            Detail(vis);
        }

        /// <summary>The wearer's level of detail group takes the robe's renderers as they are now (the game's own refresh).</summary>
        public static void Detail(VisEquipment vis)
        {
            if (vis != null)
                vis.UpdateLodgroup();
        }
    }
}
