using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DevBridge.Reload
{
    /// <summary>
    /// When a reloaded copy's changes apply. Patch logic, UI code, console commands and settings read in Awake apply
    /// at once; what a mod sets up in a patch on a world-load method (prefabs on ZNetScene.Awake, items on ObjectDB.Awake,
    /// RPCs on ZNet.Awake, ...) runs only when the next world loads, so such mods are best reloaded at the main menu.
    /// </summary>
    internal static class Lifecycle
    {
        private static readonly (Type Type, string Method, string What)[] WorldHooks =
        {
            (typeof(ZNetScene), "Awake", "prefabs"),
            (typeof(ObjectDB), "Awake", "items"),
            (typeof(ObjectDB), "CopyOtherDB", "items"),
            (typeof(ZNet), "Awake", "network"),
            (typeof(ZNet), "OnNewConnection", "peers"),
            (typeof(Game), "Awake", "world"),
            (typeof(Game), "Start", "world"),
            (typeof(ZoneSystem), "Awake", "world"),
            (typeof(ZoneSystem), "Start", "world"),
            (typeof(Game), "RequestRespawn", "first spawn"),
            (typeof(Player), "OnSpawned", "spawn"),
        };

        /// <summary>The world-load methods the assembly patches, as Type.Method (what).</summary>
        internal static List<string> Hooks(Assembly assembly)
        {
            var hooks = WorldHooks.Where(h => AccessTools.GetDeclaredMethods(h.Type).Any(m => m.Name == h.Method && PatchSweep.HasPatch(m, assembly)))
                .Select(h => $"{h.Type.Name}.{h.Method} ({h.What})").ToList();
            if (PatchSweep.HasPatch(AccessTools.Constructor(typeof(ZRoutedRpc), new[] { typeof(bool) }), assembly)) hooks.Add("ZRoutedRpc() (RPCs)");
            return hooks;
        }

        internal static bool RegistersPrefabs(List<string> hooks) => hooks.Any(h => h.Contains("(prefabs)") || h.Contains("(items)"));

        internal static string When(string state, List<string> hooks)
        {
            if (hooks.Count == 0) return "at once: the mod patches no world-load method";
            string listed = string.Join(", ", hooks);
            if (state == "menu") return $"at once; what it sets up as a world loads ({listed}) happens when you load one";
            return "patch logic, UI, console commands and settings at once; what it sets up as a world loads " +
                $"({listed}) comes only with the next world load, the old copy's stays until then: log out to the main menu " +
                "and load the world (mods with prefabs or items are best reloaded at the main menu)";
        }
    }
}
