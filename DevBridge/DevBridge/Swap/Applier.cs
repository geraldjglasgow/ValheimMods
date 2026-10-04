using System.Linq;
using UnityEngine;

namespace DevBridge.Swap
{
    /// <summary>A swap put on, and taken off, one object that draws the prefab (the prefab, a live copy, a worn item visual).</summary>
    internal static class Applier
    {
        /// <summary>Every paired renderer the object has takes its bundle look, and its animators the bundle's clips when asked.</summary>
        internal static void To(SwapEntry entry, Target target)
        {
            foreach (Pair pair in entry.Pairs.Where(p => p.Bundle))
            {
                Transform node = target.Find(pair.Path);
                Renderer live = node ? node.GetComponent<Renderer>() : null;
                if (live && Matching.Swappable(live)) One(entry, target, live, pair);
            }
            if (entry.BundleClips != null)
            {
                foreach (Animator animator in target.Root.GetComponentsInChildren<Animator>(true)) ClipSwap.Put(entry, animator, !target.IsPrefab);
            }
            entry.Covered[target.Root] = target;
        }

        private static void One(SwapEntry entry, Target target, Renderer live, Pair pair)
        {
            Keep(entry, live);
            int before = Look.Triangles(live);
            string failure = MeshSwap.Put(live, pair.Bundle, target);
            string materials = failure == null ? MaterialSwap.Put(entry, live, pair.Bundle) : null;
            Mesh mesh = Look.MeshOf(live);
            if (failure == null && mesh) entry.Meshes.Add(mesh);
            if (!target.IsPrefab) return;
            pair.Failure = failure;
            pair.Before = before;
            pair.After = Look.Triangles(live);
            pair.Materials = materials;
        }

        /// <summary>
        /// The renderer's own look, the first time the swap reaches it while it still draws it; a copy made from the
        /// swapped prefab already draws the swap, so its original is the prefab's (<see cref="Original"/>).
        /// </summary>
        private static void Keep(SwapEntry entry, Renderer live)
        {
            Mesh mesh = Look.MeshOf(live);
            if (entry.Own.ContainsKey(live) || (mesh && entry.Meshes.Contains(mesh))) return;
            entry.Own[live] = Look.Of(live);
        }

        /// <summary>What the object drew before the swap goes back on every renderer and animator of it.</summary>
        internal static void Off(SwapEntry entry, Target target)
        {
            foreach (Renderer renderer in target.Root.GetComponentsInChildren<Renderer>(true).Where(Matching.Swappable))
            {
                Look look = Original(entry, target, renderer, out bool own);
                if (look == null) continue; // never reached by the swap (a worn item, a stage asset hung on a copy)
                look.PutBack(renderer, own ? null : target);
                MaterialSwap.Restore(entry, renderer, look.Materials);
            }
            foreach (Animator animator in target.Root.GetComponentsInChildren<Animator>(true)) ClipSwap.Revert(entry, animator, !target.IsPrefab);
        }

        /// <summary>Its own recorded look; else, when it draws a swapped mesh, the prefab renderer's at the same path; else none.</summary>
        private static Look Original(SwapEntry entry, Target target, Renderer renderer, out bool own)
        {
            own = entry.Own.TryGetValue(renderer, out Look look);
            if (own) return look;
            Mesh mesh = Look.MeshOf(renderer);
            if (!mesh || !entry.Meshes.Contains(mesh)) return null;
            Transform node = Paths.Find(target.Prefab, target.PrefabPath(renderer.transform));
            Renderer prefab = node ? node.GetComponent<Renderer>() : null;
            if (prefab && entry.Own.TryGetValue(prefab, out look)) return look;
            // No prefab renderer there means no copy of one: a stage asset placed from the same bundle draws its own meshes.
            if (prefab) Debug.LogWarning($"[DevBridge] swap: {renderer.name} under {target.Root.name} draws {mesh.name} from the swap and has no original to go back to");
            return null;
        }
    }
}
