using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using DevBridge.Events;
using DevBridge.Logs;
using DevBridge.Routes;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Reload
{
    /// <summary>
    /// One reload over four frames: load the new DLL and tear the old copy down (frame 1; a DLL that does not load stops
    /// it before anything is touched), destroy the old plugin components so their OnDestroy runs after everything else's
    /// (2), start the new plugins (3), and report once their Start has run (4).
    /// </summary>
    internal sealed class ReloadJob
    {
        internal static bool Busy { get; private set; }

        internal readonly ReloadTarget Target;
        internal readonly Report Report = new Report();
        internal readonly string State = StatusRoute.State();
        internal readonly long LogMark = LogCapture.Lines.Next;
        internal NewCopy Copy;
        internal List<string> Replayed;
        internal List<string> Hooks;
        private List<string> commands;

        private ReloadJob(ReloadTarget target)
        {
            Target = target;
        }

        /// <summary>The coroutine; done gets the reply. Throws BridgeException for a reload that cannot start.</summary>
        internal static IEnumerator Run(ReloadTarget target, Action<Dictionary<string, object>> done)
        {
            Refuse();
            Busy = true;
            try
            {
                var job = new ReloadJob(target);
                job.LoadAndTearDown();
                yield return null;
                SceneSweep.DestroyPlugins(target.Plugins.Select(p => p.Instance), job.Report);
                yield return null;
                job.StartNew();
                yield return null;
                done(job.Finish());
            }
            finally
            {
                Busy = false;
            }
        }

        private static void Refuse()
        {
            if (Busy) throw new BridgeException("a reload is already running");
            string state = StatusRoute.State();
            if (state == "loading" || state == "starting") throw new BridgeException($"the game is {state}: reload at the main menu or in the world");
        }

        private void LoadAndTearDown()
        {
            Copy = CopyLoader.Load(Target.File);
            CopyLoader.Guarded(Copy, () => PluginLoader.Check(Copy, Target));
            Silence(Target.Plugins);
            commands = Teardown.Run(Target, Report);
        }

        /// <summary>
        /// Switches the old plugins off before anything is torn down: they are destroyed only a frame later, and their
        /// Update, OnGUI and coroutines would otherwise run in between and rebuild what the teardown removed.
        /// </summary>
        private static void Silence(IEnumerable<PluginInfo> plugins)
        {
            foreach (BaseUnityPlugin plugin in plugins.Select(p => p.Instance).Where(p => p))
            {
                plugin.StopAllCoroutines();
                plugin.enabled = false;
            }
        }

        private void StartNew()
        {
            PluginLoader.Start(Copy, Target, Report);
            ReloadHistory.Supersede(Target.Old, Copy.Bytes);
            foreach (PluginInfo info in Target.Plugins) ReloadHistory.Record(info.Metadata.GUID, Copy.FileTime);
            Replayed = HookReplay.Run(Copy.Assembly);
            CommandSweep.Compare(commands, Copy.Assembly, Report);
            Hooks = Lifecycle.Hooks(Copy.Assembly);
        }

        private Dictionary<string, object> Finish()
        {
            Dictionary<string, object> reply = ReloadReply.Build(this);
            EventLog.Add("reload", ReloadReply.Event(this));
            Debug.Log($"[DevBridge] reload {Target.Names}: now {Copy.Name}; removed " +
                string.Join(", ", Report.Removed.Select(p => $"{p.Value} {p.Key}")) + $"; {Report.Left.Count} notes on what was left behind");
            return reply;
        }

        /// <summary>Drives a job, logging and publishing a failure before passing it on.</summary>
        internal static IEnumerator Logged(IEnumerator job, string mod)
        {
            while (true)
            {
                bool more;
                try
                {
                    more = job.MoveNext();
                }
                catch (Exception error)
                {
                    Failed(mod, error);
                    throw;
                }
                if (!more) yield break;
                yield return job.Current;
            }
        }

        /// <summary>A failed reload: logged and published, since a watch has no caller to tell.</summary>
        internal static void Failed(string mod, Exception error)
        {
            string message = error is BridgeException ? error.Message : $"{error.GetType().Name}: {error.Message}";
            Debug.LogWarning($"[DevBridge] reload {mod} failed: {message}");
            EventLog.Add("reload", new Dictionary<string, object> { ["mod"] = mod, ["error"] = message });
        }
    }
}
