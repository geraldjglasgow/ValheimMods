using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HighSeatPlus.Meshes
{
    /// <summary>The parts of one group that one combined renderer draws: one LOD level, one material, one set of
    /// vertex channels and one set of renderer settings (<see cref="RenderRules.LookBits"/>).</summary>
    internal sealed class Bucket
    {
        public int Level;
        public Material Material;
        public int Look;
        public int Vertices;
        public readonly List<CombineInstance> Parts = new List<CombineInstance>();
    }

    /// <summary>
    /// Turns a group's buckets into a <see cref="GroupBuild"/>: each bucket combined on the CPU with
    /// Mesh.CombineMeshes (the parts' matrices already relative to the cell's centre), its bounds worked out, then
    /// uploaded with its CPU copy freed, so the combined meshes cost video memory only (about 48 bytes a vertex for the
    /// game's pieces: position, normal, tangent, one UV set). A bucket over 65,535 vertices gets 32-bit indices. For
    /// LOD-managed pieces the root gets a LOD group of the shape's one or two levels, size 1, its reference point at the
    /// middle of the pieces' own and its heights from <see cref="GroupLod"/>. The combine arrays are pooled by length up
    /// to <see cref="PooledLength"/>.
    /// </summary>
    internal static class GroupMeshes
    {
        private const int ShortIndexLimit = 65535;
        private const int PooledLength = 512;
        private const string MeshName = "HighSeatPlus combined";

        private static readonly Dictionary<int, CombineInstance[]> Arrays = new Dictionary<int, CombineInstance[]>();

        public static GroupBuild Make(CellGroup group, List<Bucket> buckets, GroupShape shape)
        {
            GameObject root = GroupBuild.MakeRoot(group.Origin);
            List<Mesh> meshes = new List<Mesh>(buckets.Count);
            List<MeshRenderer> renderers = new List<MeshRenderer>(buckets.Count);
            int vertices = 0;
            for (int i = 0; i < buckets.Count; i++)
            {
                Mesh mesh = Combine(buckets[i], ref vertices);
                meshes.Add(mesh);
                renderers.Add(GroupBuild.MakeRenderer(root, mesh, buckets[i].Material, buckets[i].Look));
            }
            if (shape.Levels == 0)
            {
                return new GroupBuild(root, meshes, renderers, null, null, shape, vertices);
            }
            LOD[] lods = Lods(shape.Levels, buckets, renderers);
            LODGroup lod = AddLod(root, lods, shape);
            return new GroupBuild(root, meshes, renderers, lod, lods, shape, vertices);
        }

        private static Mesh Combine(Bucket bucket, ref int vertices)
        {
            Mesh mesh = new Mesh { name = MeshName };
            if (bucket.Vertices > ShortIndexLimit)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }
            CombineInstance[] parts = ArrayOf(bucket.Parts.Count);
            bucket.Parts.CopyTo(parts);
            mesh.CombineMeshes(parts, true, true, false);
            Array.Clear(parts, 0, parts.Length);
            mesh.RecalculateBounds();
            vertices += mesh.vertexCount;
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static LOD[] Lods(int levels, List<Bucket> buckets, List<MeshRenderer> renderers)
        {
            LOD[] lods = new LOD[levels];
            for (int level = 0; level < levels; level++)
            {
                int count = 0;
                for (int i = 0; i < buckets.Count; i++)
                {
                    count += buckets[i].Level == level ? 1 : 0;
                }
                Renderer[] drawn = new Renderer[count];
                for (int i = 0, n = 0; i < buckets.Count; i++)
                {
                    if (buckets[i].Level == level)
                    {
                        drawn[n++] = renderers[i];
                    }
                }
                lods[level] = new LOD(1f, drawn);
            }
            return lods;
        }

        private static LODGroup AddLod(GameObject root, LOD[] lods, GroupShape shape)
        {
            LODGroup lod = root.AddComponent<LODGroup>();
            lod.fadeMode = LODFadeMode.None;
            lod.animateCrossFading = false;
            GroupLod.Fill(lods, shape.Detail, shape.Cull, shape.Radius);
            lod.SetLODs(lods);
            lod.localReferencePoint = root.transform.InverseTransformPoint(shape.Center);
            lod.size = 1f;
            return lod;
        }

        private static CombineInstance[] ArrayOf(int length)
        {
            if (length > PooledLength)
            {
                return new CombineInstance[length];
            }
            if (!Arrays.TryGetValue(length, out CombineInstance[] array))
            {
                array = new CombineInstance[length];
                Arrays[length] = array;
            }
            return array;
        }
    }
}
