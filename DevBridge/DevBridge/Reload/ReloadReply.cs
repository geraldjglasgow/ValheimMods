using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using DevBridge.Logs;

namespace DevBridge.Reload
{
    /// <summary>What a reload answers: what went, what stayed, when the change applies, and the caveats that hold.</summary>
    internal static class ReloadReply
    {
        private const LogLevel Trouble = LogLevel.Fatal | LogLevel.Error | LogLevel.Warning;

        internal static Dictionary<string, object> Build(ReloadJob job)
        {
            var reply = new Dictionary<string, object>
            {
                ["reloaded"] = job.Target.Plugins.Select(p => Versions(p, job.Copy)).ToList(),
                ["assembly"] = job.Copy.Name,
                ["file"] = job.Target.File,
                ["built"] = job.Copy.FileTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ["loadedFrom"] = job.Copy.CachePath,
                ["state"] = job.State,
                ["takesEffect"] = Lifecycle.When(job.State, job.Hooks),
                ["registersPrefabsOrItems"] = Lifecycle.RegistersPrefabs(job.Hooks),
                ["removed"] = job.Report.Removed,
                ["destroyedObjects"] = job.Report.Destroyed,
                ["consoleCommands"] = job.Report.Commands,
                ["replayed"] = job.Replayed,
                ["leftBehind"] = job.Report.Left,
                ["errorsSince"] = LogCapture.Lines.Since(job.LogMark, 20, l => (l.Level & Trouble) != 0).Select(l => Fmt.Clip(l.Text, 300)).ToList(),
                ["notes"] = Notes(job),
            };
            return reply;
        }

        /// <summary>The short form /events gets.</summary>
        internal static Dictionary<string, object> Event(ReloadJob job) => new Dictionary<string, object>
        {
            ["mod"] = job.Target.Names,
            ["assembly"] = job.Copy.Name,
            ["file"] = job.Target.File,
            ["removed"] = job.Report.Removed,
            ["leftBehind"] = job.Report.Left.Count,
            ["takesEffect"] = Lifecycle.When(job.State, job.Hooks),
        };

        private static string Versions(PluginInfo old, NewCopy copy)
        {
            PluginInfo now = copy.Plugins.First(p => p.Metadata.GUID == old.Metadata.GUID);
            return $"{old.Metadata.Name} {old.Metadata.Version} -> {now.Metadata.Version} ({old.Metadata.GUID})";
        }

        private static List<string> Notes(ReloadJob job)
        {
            var notes = new List<string>
            {
                $"Mono cannot unload an assembly: {ReloadHistory.Reloads} replaced copies stay loaded this session " +
                $"({ReloadHistory.BytesLoaded / 1024} KB of DLLs loaded by reloads, plus their compiled code); restart now and then",
            };
            if (ZNet.instance != null) notes.Add(Multiplayer(job));
            if (job.Target.Old.GetType("SyncedConfig.SyncedConfiguration") != null)
                notes.Add("Assembly.Location is now the cache copy, so SyncedConfig looks for YAML files beside it instead of beside the " +
                    "plugin DLL; those in BepInEx/config are found as before");
            return notes;
        }

        private static string Multiplayer(ReloadJob job)
        {
            string text = "Only this machine runs the new code; other players and the server keep theirs. RPC names are unchanged, " +
                "so it still talks to them, but RPCs it registers when a connection or world starts are back only with the next one";
            if (job.Target.Old.GetType("Charter.Charter") != null && !ZNet.instance.IsServer())
                text += "; settings the server binds are this machine's own until you reconnect";
            return text;
        }
    }
}
