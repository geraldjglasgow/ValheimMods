using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Swap
{
    /// <summary>The replies and events about swaps: per renderer what happened, triangles, the copies reached.</summary>
    internal static class SwapReport
    {
        private const string Covers =
            "the prefab (so every new spawn), its live copies in the world, the stage's still copies of it, the build ghost and, for " +
            "an item, the visuals characters and item stands show, on this machine only; not ragdolls (prefabs of their own), " +
            "copies a mod made of a swapped game prefab, colliders or other players' screens";

        /// <summary>The full reply to a swap: each live renderer swapped, kept or failed, each bundle renderer missing.</summary>
        internal static Dictionary<string, object> Full(SwapEntry entry)
        {
            Dictionary<string, object> reply = Head(entry);
            reply["renderers"] = entry.Pairs.Select(Line).ToList();
            reply["missing"] = entry.Missing;
            reply["triangles"] = $"{entry.Pairs.Sum(p => Before(p))} -> {entry.Pairs.Sum(p => After(p))} (its mesh renderers, every level of detail)";
            reply["clips"] = entry.Clips ? (object)entry.ClipNames.ToList() : "not asked (clips=1)";
            reply["live copies"] = entry.Count(Targets.InstanceKind);
            reply["item visuals"] = entry.Count(Targets.WornKind);
            reply["stage copies"] = entry.Count(Targets.StageKind);
            reply["build ghost"] = entry.Count(Targets.GhostKind);
            reply["covers"] = Covers;
            reply["watching"] = Watches.On(entry.Bundle);
            return reply;
        }

        private static Dictionary<string, object> Head(SwapEntry entry) => new Dictionary<string, object>
        {
            ["prefab"] = entry.PrefabName,
            ["bundle"] = entry.Bundle,
            ["asset"] = entry.Asset,
            ["materials"] = entry.Materials.ToString().ToLowerInvariant(),
        };

        private static string Line(Pair pair)
        {
            if (!pair.Bundle) return $"{Shown(pair.Path)}: kept, nothing in the bundle pairs with it ({Look.Triangles(pair.Live)} triangles)";
            if (pair.Failure != null) return $"{Shown(pair.Path)}: failed, {pair.Failure}";
            return $"{Shown(pair.Path)}: swapped (by {pair.How}), {pair.Before} -> {pair.After} triangles, {pair.Materials}";
        }

        private static string Shown(string path) => path.Length == 0 ? "(root)" : path;

        private static int Before(Pair pair) => pair.Bundle && pair.Failure == null ? pair.Before : Look.Triangles(pair.Live);

        private static int After(Pair pair) => pair.Bundle && pair.Failure == null ? pair.After : Look.Triangles(pair.Live);

        /// <summary>"9 swapped, 2 kept, 1 failed, 3 live copies".</summary>
        internal static string Counts(SwapEntry entry)
        {
            int swapped = entry.Pairs.Count(p => p.Bundle && p.Failure == null);
            int failed = entry.Pairs.Count(p => p.Bundle && p.Failure != null);
            string text = $"{swapped} swapped, {entry.Pairs.Count(p => !p.Bundle)} kept, {failed} failed, {entry.Missing.Count} missing, " +
                Reach(entry);
            return entry.Clips ? text + $", {entry.ClipNames.Count} clips" : text;
        }

        private static string Reach(SwapEntry entry) =>
            $"{entry.Count(Targets.InstanceKind)} live copies, {entry.Count(Targets.WornKind)} item visuals, {entry.Count(Targets.StageKind)} stage copies";

        /// <summary>One swap in the list: what it is, whether it is on, how far it reached.</summary>
        internal static Dictionary<string, object> Brief(SwapEntry entry)
        {
            Dictionary<string, object> brief = Head(entry);
            brief["clips"] = entry.Clips;
            brief["state"] = entry.On ? "on" : entry.Waiting;
            brief["result"] = entry.Summary;
            if (entry.On) brief["now"] = Reach(entry);
            brief["watching"] = Watches.On(entry.Bundle);
            brief["since"] = entry.At.ToString("yyyy-MM-dd HH:mm:ss");
            return brief;
        }

        /// <summary>The data of a "swap" event.</summary>
        internal static Dictionary<string, object> Event(SwapEntry entry, string action)
        {
            Dictionary<string, object> data = Head(entry);
            data["action"] = action;
            data["result"] = entry.On ? entry.Summary : entry.Waiting;
            return data;
        }
    }
}
