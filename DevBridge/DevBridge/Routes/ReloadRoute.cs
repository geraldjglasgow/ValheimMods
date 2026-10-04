using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using DevBridge.Reload;
using DevBridge.Server;

namespace DevBridge.Routes
{
    /// <summary>/reload: hot-reload a plugin's rebuilt DLL into the running game, once or whenever the build copies a new one.</summary>
    internal static class ReloadRoute
    {
        internal static void Register(Router router) => router.Add("/reload",
            "/reload?mod=<name or GUID>&file=<dll>\n" +
            "                       hot-reload a plugin: take the old copy's patches, components, console commands, RPCs,\n" +
            "                       timers and event handlers away, load the rebuilt DLL (file= or the plugins folder's) under a\n" +
            "                       new assembly name and start it; reply: what went, what stayed, when the change applies\n" +
            "/reload?list=1         the plugins that can be reloaded: DLL time against the running copy (stale), reloads, watches\n" +
            "/reload?watch=<mod>    reload by itself whenever the build copies a new DLL (polled each second); watch=off stops\n" +
            "                       every watch, watch=off&mod=<mod> one",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            string watch = request.Get("watch");
            if (request.Flag("list")) request.Json(List());
            else if (watch == "off") request.Json(Watcher.Stop(request.Get("mod")));
            else if (watch != null) request.Json(Watcher.Start(ReloadTarget.Find(watch, request.Get("file"))));
            else Reload(request);
        }

        private static void Reload(BridgeRequest request)
        {
            ReloadTarget target = ReloadTarget.Find(request.Require("mod"), request.Get("file"));
            Async.Start(request, ReloadJob.Logged(ReloadJob.Run(target, request.Json), target.Names));
        }

        private static Dictionary<string, object> List() => new Dictionary<string, object>
        {
            ["plugins"] = ReloadTarget.Reloadable().Select(Row).ToList(),
            ["watching"] = Watcher.Names,
            ["replacedCopiesLoaded"] = ReloadHistory.Reloads,
        };

        private static Dictionary<string, object> Row(PluginInfo info)
        {
            string guid = info.Metadata.GUID;
            bool exists = File.Exists(info.Location);
            return new Dictionary<string, object>
            {
                ["name"] = info.Metadata.Name,
                ["guid"] = guid,
                ["version"] = info.Metadata.Version.ToString(),
                ["assembly"] = info.Instance.GetType().Assembly.GetName().Name,
                ["file"] = info.Location,
                ["dll"] = exists ? File.GetLastWriteTime(info.Location).ToString("yyyy-MM-dd HH:mm:ss") : "missing",
                ["running"] = Running(guid, info.Location),
                ["stale"] = exists && ReloadHistory.IsStale(guid, info.Location),
                ["reloads"] = ReloadHistory.Count(guid),
                ["watched"] = Watcher.IsWatched(guid),
            };
        }

        /// <summary>The build the running copy came from, or when the game started for a DLL rebuilt since.</summary>
        private static string Running(string guid, string file)
        {
            System.DateTime time = ReloadHistory.RunningFileTime(guid, file);
            string text = time.ToString("yyyy-MM-dd HH:mm:ss");
            return ReloadHistory.Count(guid) == 0 && time == ReloadHistory.GameStarted ? $"older than {text} (game start)" : text;
        }
    }
}
