using System;
using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Sync
{
    /// <summary>Finds the DevBridges on this machine by asking each listening port of the range for /status.</summary>
    internal static class Census
    {
        /// <summary>This instance first, then the others by port; ports where nothing listens are left out.</summary>
        internal static List<Instance> Take(SyncScope scope) =>
            PeerClient.GetAll(scope.Listening(), "/status", SyncScope.ProbeMs)
                .Where(answer => !answer.Refused)
                .Select(answer => Instance.From(answer, scope.OwnPort))
                .OrderBy(instance => instance.Here ? 0 : 1)
                .ThenBy(instance => instance.Port)
                .ToList();

        /// <summary>/sync?peers=1: every instance, and the plugins whose versions differ or that some instances lack.</summary>
        internal static Dictionary<string, object> Report(List<Instance> found, SyncScope scope)
        {
            List<Instance> bridges = found.Where(instance => !instance.Foreign).ToList();
            List<Instance> ok = bridges.Where(instance => instance.Problem == null).ToList();
            List<Dictionary<string, object>> mismatches = Mismatches(ok);
            List<Dictionary<string, object>> missing = Missing(ok);
            var reply = new Dictionary<string, object>
            {
                ["same"] = bridges.Count > 1 && ok.Count == bridges.Count && mismatches.Count == 0 && missing.Count == 0,
                ["instances"] = bridges.Select(instance => instance.Full()).ToList(),
                ["mismatches"] = mismatches,
                ["missing"] = missing,
            };
            if (bridges.Count < found.Count) reply["notDevBridge"] = found.Where(instance => instance.Foreign).Select(instance => instance.Port).ToList();
            if (bridges.Count < 2) reply["note"] = NoPeers(scope);
            return reply;
        }

        internal static string NoPeers(SyncScope scope) => $"no other DevBridge answered on ports {scope.Range}";

        private static List<Dictionary<string, object>> Mismatches(List<Instance> ok) =>
            Names(ok)
                .Select(name => new { name, versions = Versions(ok, name) })
                .Where(plugin => plugin.versions.Values.Distinct().Count() > 1)
                .Select(plugin => new Dictionary<string, object> { ["plugin"] = plugin.name, ["versions"] = plugin.versions })
                .ToList();

        /// <summary>Port to version, for the instances that run the plugin.</summary>
        private static Dictionary<string, string> Versions(List<Instance> ok, string name) =>
            ok.Where(instance => instance.Plugins.ContainsKey(name)).ToDictionary(instance => instance.Port.ToString(), instance => instance.Plugins[name]);

        private static List<Dictionary<string, object>> Missing(List<Instance> ok) =>
            Names(ok)
                .Where(name => ok.Any(instance => !instance.Plugins.ContainsKey(name)))
                .Select(name => new Dictionary<string, object>
                {
                    ["plugin"] = name,
                    ["on"] = ok.Where(instance => instance.Plugins.ContainsKey(name)).Select(instance => instance.Port).ToList(),
                    ["notOn"] = ok.Where(instance => !instance.Plugins.ContainsKey(name)).Select(instance => instance.Port).ToList(),
                })
                .ToList();

        private static IEnumerable<string> Names(List<Instance> ok) =>
            ok.SelectMany(instance => instance.Plugins.Keys).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
    }
}
