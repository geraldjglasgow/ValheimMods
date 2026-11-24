using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace DevBridge.Perf
{
    /// <summary>
    /// index.json beside a recording's two files: how to read them (the record layouts as numpy types), which method and
    /// mod each id is, what probing and calibration found, and how the recording ended. Written when recording starts,
    /// so the files can be read while it runs or after a crash, and again when it ends.
    /// </summary>
    internal static class RecordIndex
    {
        internal const string FramesFile = "frames.bin";
        internal const string EntriesFile = "entries.bin";
        internal const string IndexFile = "index.json";

        private static readonly Dictionary<string, string> Kinds = new Dictionary<string, string>
        {
            ["0"] = "method on the main thread: ticks, calls and the slowest call (max) in the frame",
            ["1"] = "method on other threads: ticks and calls since the last frame (max is 0)",
            ["2"] = "mod on the main thread: ticks inside its outermost timed calls, so nested ones count once (id is a mod id)",
        };

        internal static void Write(PerfRecording recording)
        {
            var index = new Dictionary<string, object>
            {
                ["format"] = "DevBridge perf recording 1",
                ["status"] = recording.Status,
                ["started"] = recording.Started.ToString("s"),
                ["ended"] = recording.Ended == default ? null : recording.Ended.ToString("s"),
                ["options"] = Options(recording.Options),
                ["stopwatch_hz"] = Stopwatch.Frequency,
                ["files"] = new Dictionary<string, object> { ["frames"] = FramesFile, ["entries"] = EntriesFile },
                ["frame_fields"] = RecordFrames.FrameFields,
                ["entry_fields"] = RecordFrames.EntryFields,
                ["entry_kinds"] = Kinds,
                ["summary"] = recording.Summary(),
            };
            Plan(index, recording);
            string path = Path.Combine(recording.Folder, IndexFile);
            File.WriteAllText(path + ".tmp", JsonConvert.SerializeObject(index, Formatting.Indented));
            if (File.Exists(path)) File.Delete(path);
            File.Move(path + ".tmp", path);
        }

        private static void Plan(Dictionary<string, object> index, PerfRecording recording)
        {
            PerfPlan plan = recording.Plan;
            if (plan == null) return;
            index["mods"] = plan.Mods.All.Select(m => new Dictionary<string, object> { ["id"] = m.Index, ["name"] = m.Name }).ToList();
            index["methods"] = plan.Targets.Select(Method).ToList();
            index["transpilers"] = plan.Transpilers.ToDictionary(t => t.Key.Name, t => (object)t.Value.Distinct().ToList());
            index["untimeable"] = plan.Untimeable;
            index["probing"] = recording.Probing();
        }

        private static Dictionary<string, object> Method(PerfTarget target, int id) => new Dictionary<string, object>
        {
            ["id"] = id,
            ["name"] = ProbeSet.Name(target.Method),
            ["mod"] = target.Mod.Index,
            ["kind"] = string.Join("+", target.Kinds),
            ["on"] = Few(target.On, 8),
        };

        private static List<string> Few(List<MethodBase> on, int keep)
        {
            List<string> names = on.Take(keep).Select(PerfPlan.Short).ToList();
            if (on.Count > keep) names.Add($"and {on.Count - keep} more");
            return names;
        }

        private static Dictionary<string, object> Options(RecordOptions options) => new Dictionary<string, object>
        {
            ["mod"] = options.Mod,
            ["seconds"] = options.Seconds,
            ["max_mb"] = options.MaxMb,
            ["budget_ms"] = options.BudgetMs,
            ["min_us"] = options.MinMicros,
            ["name"] = options.Name,
        };

        /// <summary>%LOCALAPPDATA%\DevBridge\perf\yyyyMMdd-HHmmss[-name]: off any synced folder, as a recording can run to gigabytes.</summary>
        internal static string NewFolder(string name)
        {
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevBridge", "perf");
            string clean = new string((name ?? "").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            string folder = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd-HHmmss") + (clean.Length > 0 ? "-" + clean : ""));
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
