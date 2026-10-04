using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevBridge.Reload
{
    /// <summary>
    /// Walks what the old copy's static fields and plugin objects reach, through its own objects and plain collections,
    /// and stops what would keep running or block the new copy: timers and file watchers (the merged ConfigReload and
    /// YamlConfig poll their files on timers), and asset bundles, unloaded with their assets kept so the new copy can
    /// load the same bundle again. Threads and scene objects it reaches are reported, not touched. Each static field
    /// gets its own budget, and fields that can hold bundles go first, so one huge table never hides a bundle (a bundle
    /// left loaded makes the new copy's load of it fail). Reading a static field runs its type's static constructor if
    /// nothing did yet; that is old code, run once.
    /// </summary>
    internal sealed class StaticSweep
    {
        private const int MaxDepth = 6;
        private const int MaxItems = 2000;
        private const int RootBudget = 5000;
        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private readonly Assembly old;
        private readonly Report report;
        private readonly Func<GameObject, bool> going;
        private readonly HashSet<object> seen = new HashSet<object>(ByReference.Instance);
        private readonly List<string> held = new List<string>();
        private int budget;
        private string root;

        private StaticSweep(Assembly old, Report report, Func<GameObject, bool> going)
        {
            this.old = old;
            this.report = report;
            this.going = going;
        }

        /// <param name="going">True for a scene object the teardown is already destroying.</param>
        internal static void Run(Assembly old, IEnumerable<object> plugins, Func<GameObject, bool> going, Report report)
        {
            var sweep = new StaticSweep(old, report, going);
            foreach (FieldInfo field in StaticFields(old).OrderBy(f => HoldsBundles(f.FieldType) ? 0 : 1))
                sweep.Root($"{field.DeclaringType?.Name}.{field.Name}", () => sweep.Visit(field.GetValue(null), 0));
            foreach (object plugin in plugins) sweep.Root(plugin.GetType().Name, () => sweep.Fields(plugin, 0));
            if (sweep.held.Count > 0)
                report.Leave("scene objects the old copy's static fields point at (UI and holders it built stay until their scene unloads; " +
                    "some may be the game's own): " + Report.Tally(sweep.held));
        }

        private static List<FieldInfo> StaticFields(Assembly assembly)
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException partial) { types = partial.Types.Where(t => t != null).ToArray(); }
            var fields = new List<FieldInfo>();
            foreach (Type type in types.Where(t => !t.ContainsGenericParameters))
            {
                try
                {
                    fields.AddRange(type.GetFields(BindingFlags.Static | Declared).Where(f => !f.IsLiteral));
                }
                catch (Exception)
                {
                    // a type whose fields do not resolve holds nothing the walk could reach
                }
            }
            return fields;
        }

        private static bool HoldsBundles(Type type) =>
            typeof(AssetBundle).IsAssignableFrom(type) || (type.IsArray && HoldsBundles(type.GetElementType()))
            || (type.IsGenericType && type.GetGenericArguments().Any(HoldsBundles));

        private void Root(string name, Action walk)
        {
            root = name;
            budget = RootBudget;
            try { walk(); }
            catch (Exception error) { report.Leave($"{name} could not be read: {error.GetType().Name}"); }
            if (budget < 0) report.Leave($"{name} was too large to walk whole; the rest of it was skipped");
        }

        private void Visit(object value, int depth)
        {
            if (value == null || depth > MaxDepth || --budget < 0) return;
            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string || value is Delegate || value is MemberInfo || value is Assembly) return;
            if (!type.IsValueType && !seen.Add(value)) return;
            if (Release(value)) return;
            if (value is Object unity) Note(unity);
            else if (type.Assembly == old) Fields(value, depth);
            else if (value is IDictionary map) Items(map.Keys.Cast<object>().Concat(map.Values.Cast<object>()), depth);
            else if (value is IEnumerable list && IsPlainCollection(type)) Items(list.Cast<object>(), depth);
        }

        /// <summary>The instance fields the old copy's own classes declare (a plugin's base classes are left out).</summary>
        private void Fields(object value, int depth)
        {
            for (Type type = value.GetType(); type != null && type.Assembly == old; type = type.BaseType)
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | Declared))
                    if (!field.FieldType.IsPrimitive) Visit(field.GetValue(value), depth + 1);
        }

        // Another thread may change a collection while it is read; what was read so far is enough.
        private void Items(IEnumerable<object> items, int depth)
        {
            try
            {
                foreach (object item in items.Take(MaxItems).ToList()) Visit(item, depth + 1);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static bool IsPlainCollection(Type type) =>
            type.IsArray || (type.Namespace ?? "").StartsWith("System.Collections", StringComparison.Ordinal);

        /// <summary>Stops a timer, watcher or bundle; reports a thread. True when the value was one of those.</summary>
        private bool Release(object value)
        {
            switch (value)
            {
                case System.Threading.Timer timer: timer.Dispose(); report.Count("timers"); return true;
                case System.Timers.Timer timer: timer.Stop(); timer.Dispose(); report.Count("timers"); return true;
                case FileSystemWatcher watcher: watcher.EnableRaisingEvents = false; watcher.Dispose(); report.Count("file watchers"); return true;
                case AssetBundle bundle: Unload(bundle); return true;
                case Thread thread: if (thread.IsAlive) report.Leave($"thread '{thread.Name}' still running ({root})"); return true;
                default: return false;
            }
        }

        // Unload(false) frees the bundle's name for the new copy and keeps every asset loaded from it, which the
        // world's existing objects and the old prefabs still use.
        private void Unload(AssetBundle bundle)
        {
            if (!bundle) return;
            report.Leave($"asset bundle {bundle.name} unloaded, its loaded assets kept ({root})");
            bundle.Unload(false);
            report.Count("asset bundles");
        }

        private void Note(Object unity)
        {
            if (unity is Component component && component && component.GetType().Assembly == old) return; // the scene sweep's
            GameObject owner = unity is Component part ? (part ? part.gameObject : null) : unity as GameObject;
            if (!owner || !owner.scene.IsValid() || going(owner) || owner.GetComponentInParent<ZNetView>(true)) return;
            held.Add($"{owner.name} ({root})");
        }

        /// <summary>Identity comparison, so objects with their own Equals are still visited once each.</summary>
        private sealed class ByReference : IEqualityComparer<object>
        {
            internal static readonly ByReference Instance = new ByReference();

            public new bool Equals(object a, object b) => ReferenceEquals(a, b);

            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
