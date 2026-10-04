using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DevBridge.Reload
{
    /// <summary>
    /// Harmony patches of the old copy: exactly the patches whose methods live in its assembly are taken off, each
    /// from its original, whatever Harmony id applied them, so a shared id or another mod's patches stay untouched.
    /// </summary>
    internal static class PatchSweep
    {
        private static readonly Harmony Remover = new Harmony("com.DevBridge.reload");

        internal static void Run(Assembly old, Report report)
        {
            var failed = new List<string>();
            int foreign = 0;
            foreach (MethodBase original in Harmony.GetAllPatchedMethods().ToList())
            {
                Patches info = Harmony.GetPatchInfo(original);
                if (info == null) continue;
                if (OldCode.In(original, old)) foreign += All(info).Count(p => !OldCode.In(p.PatchMethod, old));
                foreach (MethodInfo patch in All(info).Select(p => p.PatchMethod).Where(m => OldCode.In(m, old)).Distinct().ToList())
                    Unpatch(original, patch, report, failed);
            }
            if (foreign > 0) report.Leave($"{foreign} patches of other mods on the old copy's own methods (they do not reach the new copy)");
            foreach (string line in failed.Take(5)) report.Leave("patch not removed: " + line);
        }

        private static void Unpatch(MethodBase original, MethodInfo patch, Report report, List<string> failed)
        {
            try
            {
                Remover.Unpatch(original, patch);
                report.Count("harmony patches");
            }
            catch (Exception error)
            {
                failed.Add($"{Name(patch)} on {Name(original)}: {error.Message}");
            }
        }

        internal static IEnumerable<Patch> All(Patches info) =>
            info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers).Concat(info.ILManipulators);

        /// <summary>True when the assembly has any patch on the method.</summary>
        internal static bool HasPatch(MethodBase original, Assembly assembly)
        {
            Patches info = original == null ? null : Harmony.GetPatchInfo(original);
            return info != null && All(info).Any(p => OldCode.In(p.PatchMethod, assembly));
        }

        internal static string Name(MethodBase method) => $"{method.DeclaringType?.Name}.{method.Name}";
    }
}
