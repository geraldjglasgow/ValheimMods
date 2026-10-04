using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DevBridge.Perf
{
    /// <summary>
    /// What one sample times: every prefix, postfix and finalizer another mod has put on any method, and the per-frame
    /// methods (Update, LateUpdate, FixedUpdate, OnGUI) of every MonoBehaviour in a plugin's assembly. Transpilers and IL
    /// manipulators are listed, not timed: their code is merged into the patched method. A patch method used on several
    /// methods is one target, timed across all of them.
    /// </summary>
    internal sealed class PerfPlan
    {
        private static readonly string[] FrameMethods = { "Update", "LateUpdate", "FixedUpdate", "OnGUI" };

        private readonly string filter;
        private readonly Dictionary<MethodInfo, PerfTarget> byMethod = new Dictionary<MethodInfo, PerfTarget>();

        internal readonly ModDirectory Mods = new ModDirectory();
        internal readonly List<PerfTarget> Targets = new List<PerfTarget>();
        internal readonly Dictionary<PerfMod, List<string>> Transpilers = new Dictionary<PerfMod, List<string>>();
        internal readonly List<string> Untimeable = new List<string>();
        internal readonly SortedSet<string> Seen = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        internal PerfPlan(string filter)
        {
            this.filter = filter;
            foreach (MethodBase original in Harmony.GetAllPatchedMethods().ToList()) AddPatches(original);
            foreach (Assembly assembly in Mods.PluginAssemblies) AddBehaviours(assembly);
        }

        private void AddPatches(MethodBase original)
        {
            Patches info = Harmony.GetPatchInfo(original);
            if (info == null) return;
            AddPatches(info.Prefixes, "prefix", original);
            AddPatches(info.Postfixes, "postfix", original);
            AddPatches(info.Finalizers, "finalizer", original);
            AddTranspilers(info.Transpilers.Concat(info.ILManipulators), original);
        }

        private void AddPatches(IEnumerable<Patch> patches, string kind, MethodBase original)
        {
            foreach (Patch patch in patches)
            {
                if (Own(patch)) continue;
                PerfTarget target = Target(patch.PatchMethod);
                if (target == null) continue;
                target.Kinds.Add(kind);
                if (!target.On.Contains(original)) target.On.Add(original);
            }
        }

        private void AddTranspilers(IEnumerable<Patch> patches, MethodBase original)
        {
            foreach (Patch patch in patches)
            {
                Assembly assembly = patch.PatchMethod.DeclaringType?.Assembly;
                if (Own(patch) || assembly == null) continue;
                PerfMod mod = Mods.Of(assembly);
                Seen.Add(mod.Name);
                if (!ModDirectory.Matches(mod, filter)) continue;
                if (!Transpilers.TryGetValue(mod, out List<string> on)) Transpilers[mod] = on = new List<string>();
                on.Add(Short(original));
            }
        }

        private void AddBehaviours(Assembly assembly)
        {
            foreach (Type type in Types(assembly))
            {
                if (type.ContainsGenericParameters || !typeof(MonoBehaviour).IsAssignableFrom(type)) continue;
                foreach (string name in FrameMethods)
                {
                    MethodInfo method = Declared(type, name);
                    if (method != null) Target(method)?.Kinds.Add(name);
                }
            }
        }

        /// <summary>The target for a method, made on first sight; null when it is filtered out or cannot be timed.</summary>
        private PerfTarget Target(MethodInfo method)
        {
            if (byMethod.TryGetValue(method, out PerfTarget known)) return known;
            Assembly assembly = method.DeclaringType?.Assembly;
            PerfMod mod = assembly == null ? null : Mods.Of(assembly);
            if (mod != null) Seen.Add(mod.Name);
            PerfTarget target = null;
            if (mod != null && ModDirectory.Matches(mod, filter))
            {
                if (Timeable(method)) Targets.Add(target = new PerfTarget(method, mod));
                else Untimeable.Add(ProbeSet.Name(method));
            }
            return byMethod[method] = target;
        }

        /// <summary>DevBridge's own patches and probes, by Harmony id or by assembly.</summary>
        private static bool Own(Patch patch)
        {
            string owner = patch.owner ?? "";
            if (owner == ProbeSet.HarmonyId || owner.StartsWith(DevBridgePlugin.PluginGuid, StringComparison.Ordinal)) return true;
            Assembly assembly = patch.PatchMethod.DeclaringType?.Assembly;
            return assembly == ModDirectory.Own || (assembly != null && assembly.IsDynamic);
        }

        /// <summary>A method Harmony can patch: it has a body and is not open generic. A dynamic method has no declaring type.</summary>
        private static bool Timeable(MethodInfo method)
        {
            if (method.DeclaringType == null || method.IsAbstract || method.ContainsGenericParameters) return false;
            try
            {
                return method.GetMethodBody() != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static MethodInfo Declared(Type type, string name)
        {
            try
            {
                return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    null, Type.EmptyTypes, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Every type that loads; a mod whose optional dependency is missing still gives the rest.</summary>
        private static IEnumerable<Type> Types(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException partial)
            {
                return partial.Types.Where(t => t != null);
            }
        }

        internal static string Short(MethodBase method) => $"{method.DeclaringType?.Name}.{method.Name}";
    }
}
