using System.Collections.Generic;
using System.Linq;
using DevBridge.Events;
using DevBridge.Server;

namespace DevBridge.Tune
{
    /// <summary>Every change made since the game started, numbered: listed, reverted, or printed as code to paste into a mod.</summary>
    internal static class TuneBook
    {
        private static readonly List<TuneChange> Changes = new List<TuneChange>();
        private static int next = 1;

        internal static TuneChange Find(TuneTarget target) => Changes.FirstOrDefault(change => change.Target.Key == target.Key);

        /// <summary>The change already made to this target and path, or a new one that is kept once it has written something.</summary>
        internal static TuneChange Open(TuneTarget target, object prefabBefore) => Find(target) ?? new TuneChange(next, target, prefabBefore);

        internal static void Keep(TuneChange change)
        {
            if (change.Written.Count == 0 || Changes.Contains(change)) return;
            Changes.Add(change);
            next++;
        }

        internal static List<Dictionary<string, object>> Rows() => Changes.Select(change => change.Row()).ToList();

        internal static string Code() =>
            Changes.Count == 0 ? "// no changes\n" : string.Join("\n", Changes.Select(change => change.Code())) + "\n";

        /// <summary>which: "all" (or empty) for every change, newest first, or one change's number.</summary>
        internal static List<Dictionary<string, object>> Revert(string which)
        {
            List<TuneChange> chosen = which == null || which == "all" ? Enumerable.Reverse(Changes).ToList() : new List<TuneChange> { ByNumber(which) };
            var done = new List<Dictionary<string, object>>();
            foreach (TuneChange change in chosen)
            {
                int restored = change.Revert();
                Changes.Remove(change);
                var row = new Dictionary<string, object>
                {
                    ["change"] = change.Number, ["target"] = change.Target.Label, ["path"] = change.Target.PathText,
                    ["restored"] = CodeText.Show(change.Original), ["copies"] = restored,
                };
                EventLog.Add("tune", new Dictionary<string, object>(row) { ["action"] = "revert" });
                done.Add(row);
            }
            return done;
        }

        private static TuneChange ByNumber(string which) =>
            int.TryParse(which, out int number) && Changes.FirstOrDefault(change => change.Number == number) is TuneChange change
                ? change
                : throw new BridgeException($"no change {which}; revert=all, or a number from list=1");
    }
}
