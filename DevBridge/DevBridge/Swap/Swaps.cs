using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Events;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;

namespace DevBridge.Swap
{
    /// <summary>
    /// Every prefab whose look is swapped, by prefab name: putting a swap on the prefab and everything that draws it,
    /// taking it off, reaching copies spawned since, and the hooks that keep a swap alive across a reload of its bundle.
    /// Unloading a bundle destroys every mesh, texture and clip loaded from it, so a swap is taken off before its bundle
    /// unloads and put on again from the new load in the same frame: no renderer is ever left on a destroyed mesh.
    /// </summary>
    internal static class Swaps
    {
        private static readonly Dictionary<string, SwapEntry> Entries = new Dictionary<string, SwapEntry>(StringComparer.OrdinalIgnoreCase);

        internal static IEnumerable<SwapEntry> All => Entries.Values.OrderBy(e => e.PrefabName).ToList();

        internal static string Names() => Entries.Count == 0 ? "none" : string.Join(", ", All.Select(e => e.PrefabName));

        internal static SwapEntry Get(string prefab) =>
            Entries.TryGetValue(prefab, out SwapEntry entry) ? entry : throw new BridgeException($"{prefab} is not swapped (swapped: {Names()})");

        /// <summary>Swaps a prefab's look; an earlier swap of it comes off first, once the new asset is known to be there.</summary>
        internal static void Start(SwapEntry entry, LoadedBundle loaded)
        {
            Source(loaded, entry.Asset);
            if (Entries.TryGetValue(entry.PrefabName, out SwapEntry old)) Take(old);
            Entries[entry.PrefabName] = entry;
            try
            {
                Apply(entry, loaded);
            }
            catch
            {
                Take(entry);
                throw;
            }
            SwapKeeper.Ensure();
        }

        /// <summary>Puts the swap on the prefab and everything drawing it; the entry is off whenever this is called.</summary>
        private static void Apply(SwapEntry entry, LoadedBundle loaded)
        {
            entry.Prefab = Current(entry);
            if (!entry.Prefab) throw new BridgeException($"{entry.PrefabName} is no longer a prefab here: load a world");
            GameObject source = Source(loaded, entry.Asset);
            entry.Pairs = Matching.Pairs(entry.Prefab, source, entry.Map, out entry.Missing);
            entry.BundleClips = entry.Clips ? ClipSwap.Of(loaded.Bundle) : null;
            foreach (Target target in Targets.Of(entry)) Applier.To(entry, target);
            entry.Waiting = null;
            entry.Summary = SwapReport.Counts(entry);
        }

        internal static GameObject Source(LoadedBundle loaded, string asset)
        {
            GameObject source = loaded.Bundle.LoadAsset<GameObject>(asset);
            if (source) return source;
            string prefabs = string.Join(", ", loaded.Bundle.LoadAllAssets<GameObject>().Select(g => g.name).OrderBy(n => n));
            throw new BridgeException($"{loaded.Name} has no prefab {asset} (its prefabs: {(prefabs.Length > 0 ? prefabs : "none")})");
        }

        /// <summary>
        /// The originals back on everything the swap reached or that draws the prefab now, the star looks of starred
        /// copies made again, the stage's dress originals mended; what it made goes. A prefab gone with its world (a
        /// mod's, built anew for each world) took its copies with it: there is nothing to put back.
        /// </summary>
        internal static void Revert(SwapEntry entry)
        {
            if (entry.Prefab)
            {
                List<Target> targets = Targets.Of(entry);
                var roots = new HashSet<GameObject>(targets.Select(t => t.Root));
                targets.AddRange(entry.Covered.Values.Where(t => t.Root && !roots.Contains(t.Root)));
                Dictionary<LevelEffects, int> starred = StarLooks.Read(entry, targets);
                foreach (Target target in targets.OrderBy(t => t.IsPrefab)) Applier.Off(entry, target);
                MaterialSwap.CleanLevelCache(entry);
                StarLooks.Put(starred);
                MaterialSwap.Unstage(entry);
            }
            entry.Clear();
        }

        /// <summary>Reverted and forgotten.</summary>
        internal static void Take(SwapEntry entry)
        {
            if (entry.On && entry.Prefab) Revert(entry);
            Entries.Remove(entry.PrefabName);
        }

        /// <summary>Before its bundle reloads: each swap from it comes off and waits for the new load.</summary>
        internal static void Release(LoadedBundle loaded)
        {
            foreach (SwapEntry entry in From(loaded).Where(e => e.On))
            {
                Revert(entry);
                entry.Waiting = $"waiting for {loaded.Name} to load again";
            }
        }

        /// <summary>After its bundle loaded again: each waiting swap from it goes back on; the result says how each went.</summary>
        internal static Dictionary<string, object> Restore(LoadedBundle loaded, Dictionary<string, object> result)
        {
            List<SwapEntry> waiting = From(loaded).Where(e => !e.On).ToList();
            if (waiting.Count > 0) result["swaps"] = waiting.Select(e => Again(e, loaded)).ToList();
            return result;
        }

        private static string Again(SwapEntry entry, LoadedBundle loaded)
        {
            try
            {
                Apply(entry, loaded);
                EventLog.Add("swap", SwapReport.Event(entry, "applied again"));
                return $"{entry.PrefabName}: applied again, {entry.Summary}";
            }
            catch (Exception error)
            {
                Revert(entry);
                entry.Waiting = $"not applied after {loaded.Name} loaded: {error.Message}";
                EventLog.Add("swap", SwapReport.Event(entry, "failed"));
                return $"{entry.PrefabName}: {entry.Waiting}";
            }
        }

        /// <summary>Before its bundle unloads for good: each swap from it comes off and is forgotten, and the file is no longer watched.</summary>
        internal static int Forget(LoadedBundle loaded)
        {
            List<SwapEntry> doomed = From(loaded).ToList();
            foreach (SwapEntry entry in doomed) Take(entry);
            Watches.Stop(loaded.Name);
            return doomed.Count;
        }

        private static IEnumerable<SwapEntry> From(LoadedBundle loaded) =>
            Entries.Values.Where(e => string.Equals(e.Bundle, loaded.Name, StringComparison.OrdinalIgnoreCase)).ToList();

        /// <summary>Once a second: copies of swapped prefabs spawned (or worn, shown, placed) since take the swap.</summary>
        internal static void Sweep()
        {
            foreach (SwapEntry entry in Entries.Values.Where(e => e.On).ToList())
            {
                try
                {
                    if (!Rebased(entry) && entry.Prefab) Reach(entry);
                }
                catch (Exception error)
                {
                    if (error.Message != entry.SweepError) Debug.LogWarning($"[DevBridge] swap of {entry.PrefabName}: reaching new copies failed: {error}");
                    entry.SweepError = error.Message;
                }
            }
        }

        private static void Reach(SwapEntry entry)
        {
            foreach (Target target in Targets.Of(entry).Where(t => !entry.Covered.ContainsKey(t.Root))) Applier.To(entry, target);
            foreach (GameObject gone in entry.Covered.Keys.Where(k => !k).ToList()) entry.Covered.Remove(gone);
        }

        /// <summary>
        /// A mod that builds its prefabs anew on every ZNetScene.Awake spawns from a new copy after a logout and login:
        /// the swap comes off the old copy and goes on the new one from its bundle. True when it moved. It cannot repeat:
        /// once on again the entry holds the scene's prefab, and when that fails it waits, which the sweep passes by.
        /// </summary>
        private static bool Rebased(SwapEntry entry)
        {
            GameObject now = Current(entry);
            if (!now || now == entry.Prefab) return false;
            Revert(entry);
            LoadedBundle loaded = Bundles.All.FirstOrDefault(b => string.Equals(b.Name, entry.Bundle, StringComparison.OrdinalIgnoreCase));
            if (loaded != null) Again(entry, loaded);
            else entry.Waiting = $"waiting for {entry.Bundle} to load again";
            return true;
        }

        /// <summary>The prefab the scene spawns under that name now, else the one swapped.</summary>
        private static GameObject Current(SwapEntry entry)
        {
            GameObject now = ZNetScene.instance ? ZNetScene.instance.GetPrefab(entry.Hash) : null;
            return now ? now : entry.Prefab;
        }
    }
}
