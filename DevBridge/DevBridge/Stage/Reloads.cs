using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Studio;
using DevBridge.Swap;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>
    /// Reloading and unloading a bundle with things placed from it. Everything made from the bundle is destroyed before
    /// it unloads (with all its loaded objects); after a reload each asset is made again at the same place with the same
    /// id and dresses, and clip swaps that used it are put back. Effects and sounds from it are not replayed. Prefab
    /// swaps from it (/swap) come off before it unloads and go back on from the new load in the same frame.
    /// </summary>
    internal static class Reloads
    {
        internal static Dictionary<string, object> Reload(LoadedBundle loaded)
        {
            var assets = Placements.All.Where(p => p.Kind == Placement.Asset && From(p, loaded))
                .Select(p => new { Placement = p, p.Root.transform.position, p.Root.transform.rotation }).ToList();
            List<int> dropped = Placements.All.Where(p => p.Kind != Placement.Asset && From(p, loaded)).Select(p => p.Id).ToList();
            List<Placement> swapped = Placements.All.Where(p => p.SwapBundles.Contains(loaded.Name)).ToList();
            Placements.Clear(p => dropped.Contains(p.Id)); // before the assets go, so the row is not reset as the list empties
            foreach (var asset in assets) asset.Placement.Destroy(now: true);
            foreach (Placement placement in swapped) Poses.Unswap(placement, keepSpec: true);
            Swaps.Release(loaded);
            var lost = new List<string>();
            int reattached = StudioAttach.Across(loaded.Name, () => Bundles.Refresh(loaded), lost);
            foreach (var asset in assets) Try(() => Stager.Rebuild(asset.Placement, asset.position, asset.rotation), asset.Placement, lost);
            foreach (Placement placement in swapped.Where(p => p.Alive)) Try(() => Reswap(placement), placement, lost);
            return Swaps.Restore(loaded, new Dictionary<string, object>
            {
                ["replaced"] = assets.Select(a => a.Placement).Where(p => p.Alive).Select(p => p.Id).ToList(),
                ["reswapped"] = swapped.Where(p => p.Alive && p.SwapSpec != null).Select(p => p.Id).ToList(),
                ["dropped effects and sounds"] = dropped,
                ["studio effects attached again"] = reattached,
                ["lost"] = lost,
            });
        }

        internal static Dictionary<string, object> Unload(LoadedBundle loaded)
        {
            foreach (Placement placement in Placements.All.Where(p => p.SwapBundles.Contains(loaded.Name)).ToList())
                Poses.Unswap(placement, keepSpec: false);
            int removed = Placements.Clear(p => From(p, loaded));
            int reverted = Swaps.Forget(loaded);
            int detached = StudioAttach.Lift(loaded.Name).Count;
            Bundles.Drop(loaded);
            return new Dictionary<string, object>
            {
                ["unloaded"] = loaded.Name, ["removed"] = removed, ["swaps reverted"] = reverted, ["studio effects removed"] = detached,
            };
        }

        private static bool From(Placement placement, LoadedBundle loaded) =>
            string.Equals(placement.Bundle, loaded.Name, StringComparison.OrdinalIgnoreCase);

        private static void Reswap(Placement placement)
        {
            string spec = placement.SwapSpec;
            placement.SwapSpec = null;
            Poses.Swap(placement, Poses.Of(placement), spec);
        }

        // One asset that no longer loads (renamed or gone from the new build) does not stop the others.
        private static void Try(Action action, Placement placement, List<string> lost)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                lost.Add($"{placement.Id} {placement.Name}: {error.Message}");
                if (placement.Kind == Placement.Asset) placement.Destroy();
                else Poses.Unswap(placement, keepSpec: false);
            }
        }
    }
}
