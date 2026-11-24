using System;
using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Perf
{
    /// <summary>A recording at a glance, for /perf/record's replies, the index and the perf event: the full story is in its files.</summary>
    internal static class RecordSummary
    {
        internal static Dictionary<string, object> Of(PerfRecording recording, RecordFrames recorder, long bytes)
        {
            var summary = new Dictionary<string, object>
            {
                ["state"] = recording.State,
                ["status"] = recording.Status,
                ["folder"] = recording.Folder,
                ["methods"] = recording.Plan?.Targets.Count ?? 0,
                ["mb"] = Math.Round(bytes / 1048576.0, 1),
            };
            if (recorder == null || recorder.Frames == 0) return summary;
            summary["frames"] = recorder.Frames;
            summary["seconds"] = Math.Round(recorder.Seconds, 1);
            summary["fps"] = Math.Round(recorder.Frames / Math.Max(recorder.Seconds, 1e-3), 1);
            summary["max_ms"] = Math.Round(recorder.MaxMs, 1);
            summary["frames_over_50ms"] = recorder.Over50;
            summary["dropped_frames"] = recorder.Dropped;
            summary["top_mods_ms_per_frame"] = TopMods(recording.Plan, recorder);
            return summary;
        }

        private static List<string> TopMods(PerfPlan plan, RecordFrames recorder) => plan.Mods.All
            .Where(m => recorder.ModTicks[m.Index] > 0)
            .OrderByDescending(m => recorder.ModTicks[m.Index])
            .Take(8)
            .Select(m => $"{m.Name} {RecordFrames.Ms(recorder.ModTicks[m.Index]) / recorder.Frames:0.###}")
            .ToList();
    }
}
