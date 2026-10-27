using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// One of a launched ship's hull meshes (<see cref="KwHoleLooks"/>: a chunk of a few parts' looks in one material,
    /// <see cref="KwHoleChunks"/>), in the look's frame: its renderer's filter, its whole mesh and its bounds. At its first
    /// hole the whole mesh is read into lists (<see cref="KwHoleMeshes"/>) and its holes are cut from then on by
    /// <see cref="KwHolePatches"/>; with none it wears the whole mesh again.
    /// </summary>
    public sealed class KwHolePart
    {
        private readonly Vector3 low, high;
        private KwHolePatches? patches;
        private Mesh? holed;

        public KwHolePart(MeshFilter filter, Mesh whole)
        {
            (Filter, Whole) = (filter, whole);
            (low, high) = (whole.bounds.min, whole.bounds.max);
        }

        public MeshFilter Filter { get; }
        public Mesh Whole { get; }

        /// <summary>The keys of the holes it was last cut with, joined; "" whole.</summary>
        public string Key { get; private set; } = "";

        /// <summary>Whether a hole comes near enough to cut it: within its outline's reach and depth of its bounds.</summary>
        public bool Near(KwHoleShape hole)
        {
            float reach = hole.Reach + KwHoleCut.Inside;
            Vector3 c = hole.Centre;
            return c.x > low.x - reach && c.x < high.x + reach && c.y > low.y - reach && c.y < high.y + reach && c.z > low.z - reach && c.z < high.z + reach;
        }

        /// <summary>Wears the whole mesh with these holes (in its frame, each with its key) cut through it; whole again with none.</summary>
        public void Cut(List<KwHoleShape> holes, List<string> keys)
        {
            Key = string.Join(";", keys);
            if (holes.Count == 0 || !Whole.isReadable)
            {
                Filter.sharedMesh = Whole;
                patches?.Cut(holes, keys, out _);
                return;
            }
            patches ??= new KwHolePatches(KwHoleMeshes.Read(Whole));
            if (!patches.Cut(holes, keys, out KwHoleMesh? cut))
                return;
            if (cut == null)
            {
                Filter.sharedMesh = Whole;
                return;
            }
            holed ??= new Mesh { name = Whole.name + "_holed" };
            KwHoleMeshes.Write(cut, holed);
            Filter.sharedMesh = holed;
        }

        public void Let()
        {
            Object.Destroy(Whole);
            if (holed != null)
                Object.Destroy(holed);
        }
    }

    /// <summary>
    /// A mesh's holes cut a group at a time: the holes whose triangles round them meet are cut together, each group's cut
    /// kept as a patch by its holes' keys (<see cref="KwHoleCut.Patch"/>), so a hit cuts only the group it changed and the
    /// mesh is the whole one with every group's patch on it (<see cref="KwHoleCut.Join"/>). The triangles round each hole
    /// come from an index of the mesh's triangles made once (<see cref="KwHoleIndex"/>). Pure.
    /// </summary>
    public sealed class KwHolePatches
    {
        private readonly KwHoleMesh data;
        private readonly KwHoleIndex index;
        private Dictionary<string, KwHolePatch> patches = new Dictionary<string, KwHolePatch>();
        private readonly Dictionary<string, HashSet<long>> near = new Dictionary<string, HashSet<long>>();
        private string joined = "";

        public KwHolePatches(KwHoleMesh data) => (this.data, index) = (data, new KwHoleIndex(data));

        /// <summary>
        /// The whole mesh with these holes cut through it (each with its key): false when the patches that change it are the
        /// ones it was last joined with (nothing to do); cut null when none changes it (the whole mesh). Patches of holes
        /// gone are let go; a hole whose outline reaches none of the mesh's triangles changes nothing.
        /// </summary>
        public bool Cut(List<KwHoleShape> holes, List<string> keys, out KwHoleMesh? cut)
        {
            Dictionary<string, KwHolePatch> now = new Dictionary<string, KwHolePatch>();
            foreach (List<int> group in Groups(holes, keys).Where(g => g.Exists(i => near[keys[i]].Count > 0)))
            {
                string key = string.Join("|", group.Select(i => keys[i]));
                now[key] = patches.TryGetValue(key, out KwHolePatch kept) ? kept : KwHoleCut.Patch(data, group.ConvertAll(i => holes[i]), true, index);
            }
            foreach (string gone in near.Keys.Where(k => !keys.Contains(k)).ToList())
                near.Remove(gone);
            patches = now;
            List<string> changing = now.Where(p => !p.Value.Empty).Select(p => p.Key).ToList();
            cut = null;
            if (string.Join(";", changing) == joined)
                return false;
            joined = string.Join(";", changing);
            cut = changing.Count > 0 ? KwHoleCut.Join(data, changing.ConvertAll(k => now[k])) : null;
            return true;
        }

        // The holes in groups whose triangles round them (KwHoleIndex.Near) meet, by joining any two that share one.
        private List<List<int>> Groups(List<KwHoleShape> holes, List<string> keys)
        {
            List<HashSet<long>> sets = holes.Select((h, i) => Near(h, keys[i])).ToList();
            int[] group = Enumerable.Range(0, holes.Count).ToArray();
            for (int i = 0; i < holes.Count; i++)
            {
                for (int j = i + 1; j < holes.Count; j++)
                {
                    if (group[j] == group[i] || !sets[i].Overlaps(sets[j]))
                        continue;
                    int from = group[j];
                    for (int k = 0; k < group.Length; k++)
                        group[k] = group[k] == from ? group[i] : group[k];
                }
            }
            return Enumerable.Range(0, holes.Count).GroupBy(i => group[i]).Select(g => g.ToList()).ToList();
        }

        // The triangles round a hole, each submesh's starts, worked out once a hole.
        private HashSet<long> Near(KwHoleShape hole, string key)
        {
            if (near.TryGetValue(key, out HashSet<long> found))
                return found;
            found = new HashSet<long>();
            for (int s = 0; s < data.Subs.Count; s++)
            {
                foreach (int t in index.Near(s, new List<KwHoleShape> { hole }))
                    found.Add(((long)s << 32) | (uint)t);
            }
            return near[key] = found;
        }
    }
}
