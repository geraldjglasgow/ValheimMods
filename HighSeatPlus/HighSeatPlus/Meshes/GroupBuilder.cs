using System.Collections.Generic;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// Rebuilds a group. Members that have settled and still look as they did go in; a member that changed is looked
    /// at again first and may stay, move to another group, wait or stay with the game; members still settling keep
    /// drawing themselves and the group comes back once they have settled. Parts are sorted into one bucket per LOD
    /// level, material, set of vertex channels (so no mesh is given the defaults Unity fills in for a channel it lacks)
    /// and renderer settings, and handed to <see cref="GroupMeshes"/>. The swap happens in one frame: the new build shown, its pieces
    /// turned off, members it does not hold turned back on, the old build destroyed. Builds stop at
    /// <see cref="MaxVertices"/> combined vertices in all (about 140 MB of video memory at 48 bytes a vertex); a group
    /// that does not fit draws as the game draws it and is tried again after <see cref="PieceRegistry.RetrySeconds"/>.
    /// </summary>
    internal static class GroupBuilder
    {
        private const int MaxVertices = 3000000;

        private static readonly List<PieceEntry> Included = new List<PieceEntry>();
        private static readonly List<Bucket> Buckets = new List<Bucket>();
        private static readonly Stack<Bucket> Spare = new Stack<Bucket>();
        private static readonly Dictionary<long, Bucket> ByKey = new Dictionary<long, Bucket>();
        private static int _stamp;
        private static bool _warned;

        /// <summary>Rebuilds the group from its settled members; returns the vertices it set out to combine.</summary>
        public static int Rebuild(CellGroup group, float now)
        {
            Collect(group, now);
            if (group.Members.Count == 0 || Included.Count == 0)
            {
                Swap(group, null);
                Release();
                return 0;
            }
            GroupShape shape = Shape(group.Key.Lod != 0);
            int vertices = FillBuckets(group.Origin, Mathf.Max(shape.Levels, 1));
            int held = group.Build != null ? group.Build.Vertices : 0;
            bool fits = CellGroups.Vertices - held + vertices <= MaxVertices;
            GroupBuild build = fits ? GroupMeshes.Make(group, Buckets, shape) : null;
            if (!fits)
            {
                Defer(group, now);
            }
            Swap(group, build);
            Release();
            return vertices;
        }

        private static void Collect(CellGroup group, float now)
        {
            Included.Clear();
            for (int i = group.Members.Count - 1; i >= 0; i--)
            {
                PieceEntry member = group.Members[i];
                if (!member.Settled(now))
                {
                    group.WaitFor(member);
                }
                else if (PartChecks.Valid(member) || Recheck(group, member, now))
                {
                    Included.Add(member);
                }
            }
            if (group.Members.Count == 0)
            {
                CellGroups.Discard(group);
            }
        }

        // A member that changed: looked at again with its parts back on; it stays when it still belongs here.
        private static bool Recheck(CellGroup group, PieceEntry member, float now)
        {
            member.SetForced(false);
            Verdict verdict = PieceInspector.Inspect(member);
            if (verdict == Verdict.Combine && member.Key.Equals(group.Key))
            {
                return true;
            }
            group.Drop(member);
            PieceRegistry.Place(member, verdict, now);
            return false;
        }

        // Two levels when any piece has a simple level of its own, else one; the largest reaches; the middle of the
        // pieces' own LOD reference points and the farthest of them from it.
        private static GroupShape Shape(bool managed)
        {
            GroupShape shape = new GroupShape();
            Vector3 min = Included[0].LodPoint;
            Vector3 max = min;
            for (int i = 0; i < Included.Count; i++)
            {
                PieceEntry member = Included[i];
                min = Vector3.Min(min, member.LodPoint);
                max = Vector3.Max(max, member.LodPoint);
                shape.Detail = Mathf.Max(shape.Detail, member.DetailReach);
                shape.Cull = Mathf.Max(shape.Cull, member.CullReach);
            }
            shape.Center = (min + max) * 0.5f;
            for (int i = 0; i < Included.Count; i++)
            {
                shape.Radius = Mathf.Max(shape.Radius, (Included[i].LodPoint - shape.Center).magnitude);
            }
            shape.Levels = !managed ? 0 : shape.Detail > 0f ? 2 : 1;
            return shape;
        }

        private static int FillBuckets(Vector3 origin, int levels)
        {
            Matrix4x4 toCell = Matrix4x4.Translate(-origin);
            int total = 0;
            for (int m = 0; m < Included.Count; m++)
            {
                PieceEntry member = Included[m];
                for (int p = 0; p < member.Parts.Count; p++)
                {
                    Part part = member.Parts[p];
                    total += AddPart(member, in part, toCell, levels);
                }
            }
            return total;
        }

        private static int AddPart(PieceEntry member, in Part part, Matrix4x4 toCell, int levels)
        {
            MeshInfo info = RenderRules.Info(part.Mesh);
            Matrix4x4 matrix = toCell * part.Matrix;
            int added = 0;
            for (int level = 0; level < levels; level++)
            {
                for (int s = 0; (part.Levels & (1 << level)) != 0 && s < part.MaterialCount; s++)
                {
                    Bucket bucket = BucketFor(level, member.Materials[part.MaterialStart + s], info.Attributes, part.Look);
                    bucket.Parts.Add(new CombineInstance { mesh = part.Mesh, subMeshIndex = s, transform = matrix });
                    bucket.Vertices += info.Vertices;
                    added += info.Vertices;
                }
            }
            return added;
        }

        // Material id in bits 0-31, vertex channels 32-38, renderer settings 39-54, LOD level 55-56.
        private static Bucket BucketFor(int level, Material material, int attributes, int look)
        {
            long key = ((long)level << 55) | ((long)(look & 0xFFFF) << 39) | ((long)attributes << 32) | (uint)material.GetInstanceID();
            if (ByKey.TryGetValue(key, out Bucket bucket))
            {
                return bucket;
            }
            bucket = Spare.Count > 0 ? Spare.Pop() : new Bucket();
            bucket.Level = level;
            bucket.Material = material;
            bucket.Look = look;
            bucket.Vertices = 0;
            ByKey.Add(key, bucket);
            Buckets.Add(bucket);
            return bucket;
        }

        // Members that went into the build are turned off with it shown; the others draw themselves.
        private static void Swap(CellGroup group, GroupBuild build)
        {
            _stamp++;
            for (int i = 0; build != null && i < Included.Count; i++)
            {
                Included[i].BuildStamp = _stamp;
            }
            GroupBuild old = group.Build;
            group.Build = build;
            group.Open = false;
            int held = 0;
            for (int i = 0; i < group.Members.Count; i++)
            {
                PieceEntry member = group.Members[i];
                member.InBuild = member.BuildStamp == _stamp;
                member.SetForced(member.InBuild);
                held += member.InBuild ? 1 : 0;
            }
            group.Dirty = held < group.Members.Count;
            build?.SetShown(true);
            CellGroups.Replaced(old, build);
        }

        private static void Defer(CellGroup group, float now)
        {
            group.QuietUntil = Mathf.Max(group.QuietUntil, now + PieceRegistry.RetrySeconds);
            if (!_warned)
            {
                _warned = true;
                HighSeatPlus.Log.LogInfo("Combined building meshes reached " + MaxVertices + " vertices; more buildings are drawn by the game until some unload");
            }
        }

        private static void Release()
        {
            for (int i = 0; i < Buckets.Count; i++)
            {
                Buckets[i].Parts.Clear();
                Buckets[i].Material = null;
                Spare.Push(Buckets[i]);
            }
            Buckets.Clear();
            ByKey.Clear();
            Included.Clear();
        }
    }
}
