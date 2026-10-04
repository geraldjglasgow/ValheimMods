using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevBridge.Routes;
using UnityEngine;

namespace DevBridge.Reload
{
    /// <summary>
    /// Watched plugins reload by themselves when their DLL changes: the file's write time is polled once a second, and
    /// a changed file is reloaded once its time and size held still for a poll and it opens for reading (the build has
    /// finished copying). A failed reload is logged and published; the next build tries again. A DLL holding several
    /// plugins is one watch, found by any of their GUIDs.
    /// </summary>
    internal static class Watcher
    {
        private sealed class Watch
        {
            internal string Guid;
            internal HashSet<string> Guids;
            internal string Name;
            internal string File;
            internal DateTime Handled;
            internal DateTime Seen;
            internal long Size = -1;
        }

        private static readonly List<Watch> Watches = new List<Watch>();
        private static bool looping;

        internal static bool IsWatched(string guid) => Watches.Any(w => w.Guids.Contains(guid));

        internal static List<string> Names => Watches.Select(w => $"{w.Name} ({w.File})").ToList();

        /// <summary>Starts watching; a DLL already newer than the running copy reloads on the first polls.</summary>
        internal static Dictionary<string, object> Start(ReloadTarget target)
        {
            var guids = new HashSet<string>(target.Plugins.Select(p => p.Metadata.GUID), StringComparer.OrdinalIgnoreCase);
            Watches.RemoveAll(w => w.Guids.Overlaps(guids));
            Watches.Add(new Watch
            {
                Guid = target.Guid,
                Guids = guids,
                Name = target.Names,
                File = target.File,
                Handled = ReloadHistory.RunningFileTime(target.Guid, target.File),
            });
            if (!looping)
            {
                looping = true;
                DevBridgePlugin.Instance.StartCoroutine(Loop());
            }
            return Reply($"watching {target.Names}: {target.File}");
        }

        internal static Dictionary<string, object> Stop(string mod)
        {
            if (mod == null) Watches.Clear();
            else
            {
                string guid = ReloadTarget.Lookup(mod).Metadata.GUID;
                Watches.RemoveAll(w => w.Guids.Contains(guid));
            }
            return Reply(mod == null ? "stopped every watch" : $"stopped watching {mod}");
        }

        private static Dictionary<string, object> Reply(string done) =>
            new Dictionary<string, object> { ["done"] = done, ["watching"] = Names };

        private static IEnumerator Loop()
        {
            var second = new WaitForSecondsRealtime(1f);
            while (Watches.Count > 0)
            {
                yield return second;
                Watch due = ReloadJob.Busy || !Settled() ? null : Watches.FirstOrDefault(Due);
                if (due == null) continue;
                due.Handled = due.Seen;
                IEnumerator run = Guarded(due);
                while (run.MoveNext()) yield return run.Current;
            }
            looping = false;
        }

        private static bool Settled()
        {
            string state = StatusRoute.State();
            return state == "menu" || state == "ingame";
        }

        /// <summary>True when the file changed since the last reload and has not changed since the previous poll.</summary>
        private static bool Due(Watch watch)
        {
            var info = new FileInfo(watch.File);
            if (!info.Exists || info.LastWriteTime == watch.Handled) return false;
            bool still = info.LastWriteTime == watch.Seen && info.Length == watch.Size;
            watch.Seen = info.LastWriteTime;
            watch.Size = info.Length;
            return still && Readable(watch.File);
        }

        private static bool Readable(string file)
        {
            try
            {
                using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>Runs the reload; a failure is logged and published by the job and ends here.</summary>
        private static IEnumerator Guarded(Watch watch)
        {
            IEnumerator job;
            try
            {
                job = ReloadJob.Logged(ReloadJob.Run(ReloadTarget.Find(watch.Guid, watch.File), _ => { }), watch.Name);
            }
            catch (Exception error)
            {
                ReloadJob.Failed(watch.Name, error);
                yield break;
            }
            while (true)
            {
                try
                {
                    if (!job.MoveNext()) yield break;
                }
                catch (Exception)
                {
                    yield break;
                }
                yield return job.Current;
            }
        }
    }
}
