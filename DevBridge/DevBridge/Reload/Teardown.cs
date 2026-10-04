using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace DevBridge.Reload
{
    /// <summary>
    /// The first frame of a reload: everything of the old copy except its plugin components, which go a frame later.
    /// Patches come off first, so no old patch runs while the rest goes. A step that fails is reported and the others
    /// still run: half a teardown is better than an old copy left fully running beside the new one.
    /// </summary>
    internal static class Teardown
    {
        /// <summary>Returns the console commands removed, to see later which the new copy registered again.</summary>
        internal static List<string> Run(ReloadTarget target, Report report)
        {
            Assembly old = target.Old;
            List<BaseUnityPlugin> plugins = target.Plugins.Select(p => p.Instance).ToList();
            List<string> commands = new List<string>();
            Step("harmony patches", () => PatchSweep.Run(old, report), report);
            Step("console commands", () => commands = CommandSweep.Remove(old), report);
            Step("RPCs", () => RpcSweep.Run(old, report), report);
            Step("event handlers", () => EventSweep.Run(old, report), report);
            SceneSweep scene = null;
            Step("components", () => scene = SceneSweep.Run(old, plugins, Names(target), report), report);
            Step("static fields", () => StaticSweep.Run(old, plugins, go => scene != null && scene.Going(go), report), report);
            Step("timers", () => TimerSweep.Run(old, report), report);
            Step("leftovers", () => Leftovers.Run(old, scene?.PrefabParts ?? new HashSet<GameObject>(), report), report);
            Step("dependents", () => Leftovers.Dependents(target, report), report);
            return commands;
        }

        /// <summary>The names a DontDestroyOnLoad root of the mod would start with: plugin names and the DLL name.</summary>
        private static IEnumerable<string> Names(ReloadTarget target) =>
            target.Plugins.Select(p => p.Metadata.Name.Replace(" ", "")).Append(ReloadTarget.OriginalName(target.Plugins[0])).Distinct();

        private static void Step(string what, Action action, Report report)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                report.Leave($"{what} step failed, the rest went on: {error.GetType().Name}: {error.Message}");
            }
        }
    }
}
