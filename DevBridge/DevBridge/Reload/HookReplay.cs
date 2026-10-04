using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevBridge.Routes;
using HarmonyLib;

namespace DevBridge.Reload
{
    /// <summary>
    /// Runs the new copy's patches on game methods that already ran and will not run again soon: Terminal.InitTerminal
    /// (once per process; the mods here register their console commands in its postfix) and, at the main menu,
    /// FejdStartup.Start (YamlConfig loads its files there). Only prefixes and postfixes whose parameters are
    /// __instance or __originalMethod are called; any other is listed as skipped.
    /// </summary>
    internal static class HookReplay
    {
        internal static List<string> Run(Assembly current)
        {
            var lines = new List<string>();
            if (Terminal.m_terminalInitialized) Replay(AccessTools.Method(typeof(Terminal), "InitTerminal"), null, current, lines);
            if (FejdStartup.instance && StatusRoute.State() == "menu")
                Replay(AccessTools.Method(typeof(FejdStartup), "Start"), FejdStartup.instance, current, lines);
            return lines;
        }

        private static void Replay(MethodBase original, object instance, Assembly current, List<string> lines)
        {
            Patches info = original == null ? null : Harmony.GetPatchInfo(original);
            if (info == null) return;
            foreach (Patch patch in info.Prefixes.Concat(info.Postfixes).Where(p => OldCode.In(p.PatchMethod, current)))
                lines.Add($"{PatchSweep.Name(original)}: {PatchSweep.Name(patch.PatchMethod)} {Invoke(patch.PatchMethod, original, instance)}");
        }

        private static string Invoke(MethodInfo method, MethodBase original, object instance)
        {
            var args = new List<object>();
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (parameter.Name == "__instance") args.Add(instance);
                else if (parameter.Name == "__originalMethod") args.Add(original);
                else return $"skipped (cannot fill {parameter.Name})";
            }
            try
            {
                method.Invoke(null, args.ToArray());
                return "ran";
            }
            catch (TargetInvocationException error)
            {
                return $"threw {error.InnerException?.GetType().Name}: {error.InnerException?.Message}";
            }
        }
    }
}
