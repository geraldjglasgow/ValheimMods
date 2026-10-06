using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using DevBridge.Perf;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Heap
{
    /// <summary>
    /// /heap?statics=1: the static collections (arrays, lists, dictionaries, sets: anything with a Count) of every
    /// plugin's assembly, merged libraries included, by size: items, and nested = the items of the collections it holds,
    /// one level down. What a mod keeps forever is what a collection must walk every time. A class whose static
    /// constructor threw is listed under broken_types (it fails on every use after). Reading a static runs its class's
    /// static constructor if nothing has used the class yet, as a first use in the game would.
    /// </summary>
    internal static class StaticCensus
    {
        private const BindingFlags Statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const int NestedLimit = 1_000_000;
        private static readonly string[] Columns = { "items", "nested" };
        private static readonly Dictionary<Type, PropertyInfo> countProperties = new Dictionary<Type, PropertyInfo>();

        internal static Dictionary<string, object> Reply(BridgeRequest request)
        {
            Stopwatch watch = Stopwatch.StartNew();
            var rows = new Dictionary<string, long[]>();
            var broken = new List<string>();
            var mods = new ModDirectory();
            foreach (Assembly assembly in mods.PluginAssemblies)
            {
                PerfMod mod = mods.Of(assembly);
                if (ModDirectory.Matches(mod, request.Get("mod"))) Walk(assembly, mod.Name, rows, broken);
            }
            Dictionary<string, object> reply = Present(rows, request);
            reply["broken_types"] = broken;
            reply["census_ms"] = watch.ElapsedMilliseconds;
            return reply;
        }

        private static Dictionary<string, object> Present(Dictionary<string, long[]> rows, BridgeRequest request)
        {
            int top = Mathf.Clamp(request.Int("top", 25), 1, 1000);
            string mark = request.Get("mark"), diff = request.Get("diff");
            var reply = new Dictionary<string, object> { ["mods"] = PerMod(rows) };
            if (diff != null) reply["changes"] = HeapMarks.Diff("statics", diff, rows, Columns, r => Math.Abs(r[0]) + Math.Abs(r[1]), top);
            else reply["fields"] = rows.OrderByDescending(r => r.Value[0] + r.Value[1]).Take(top)
                .Select(r => new Dictionary<string, object> { ["field"] = r.Key, ["items"] = r.Value[0], ["nested"] = r.Value[1] }).ToList();
            if (mark != null) HeapMarks.Save("statics", mark, rows);
            return reply;
        }

        /// <summary>Each mod's static collections: how many, and their items and nested items together.</summary>
        private static List<Dictionary<string, object>> PerMod(Dictionary<string, long[]> rows) =>
            rows.GroupBy(r => r.Key.Substring(0, r.Key.IndexOf(": ", StringComparison.Ordinal)))
                .Select(g => (mod: g.Key, fields: g.Count(), items: g.Sum(r => r.Value[0]), nested: g.Sum(r => r.Value[1])))
                .OrderByDescending(m => m.items + m.nested)
                .Select(m => new Dictionary<string, object> { ["mod"] = m.mod, ["collections"] = m.fields, ["items"] = m.items, ["nested"] = m.nested })
                .ToList();

        private static void Walk(Assembly assembly, string mod, Dictionary<string, long[]> rows, List<string> broken)
        {
            foreach (Type type in Types(assembly))
            {
                if (type.ContainsGenericParameters || type.Name.StartsWith("<", StringComparison.Ordinal)) continue;
                Fields(type, mod, rows, broken);
            }
        }

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

        private static void Fields(Type type, string mod, Dictionary<string, long[]> rows, List<string> broken)
        {
            foreach (FieldInfo field in type.GetFields(Statics))
            {
                if (field.IsLiteral || !Collectionish(field.FieldType)) continue;
                object value;
                try
                {
                    value = field.GetValue(null);
                }
                catch (Exception error)
                {
                    Exception cause = error.GetBaseException();
                    broken.Add($"{mod}: {type.FullName}: {cause.GetType().Name}: {cause.Message}");
                    return;
                }
                long items = CountOf(value);
                if (items >= 0) rows[$"{mod}: {type.FullName}.{field.Name}"] = new[] { items, Nested(value) };
            }
        }

        private static bool Collectionish(Type type) =>
            type.IsArray || (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type));

        /// <summary>Items in a collection: an array's length, a Count property; -1 for anything that only enumerates.</summary>
        private static long CountOf(object value)
        {
            if (value == null) return 0;
            if (value is Array array) return array.LongLength;
            if (value is ICollection collection) return collection.Count;
            PropertyInfo count = CountProperty(value.GetType());
            return count == null ? -1 : (int)count.GetValue(value, null);
        }

        private static PropertyInfo CountProperty(Type type)
        {
            if (countProperties.TryGetValue(type, out PropertyInfo found)) return found;
            PropertyInfo count = type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance, null, typeof(int), Type.EmptyTypes, null);
            return countProperties[type] = count;
        }

        /// <summary>The items of the collections inside a collection (a dictionary's values), one level down.</summary>
        private static long Nested(object value)
        {
            if (value == null || value is string) return 0;
            IEnumerable elements = value is IDictionary dictionary ? dictionary.Values : value as IEnumerable;
            long total = 0, seen = 0;
            try
            {
                foreach (object element in elements)
                {
                    if (++seen > NestedLimit) break;
                    if (element is IEnumerable && !(element is string)) total += Math.Max(0, CountOf(element));
                }
            }
            catch (InvalidOperationException)
            {
                // Changed while being read (another thread): what was counted stands.
            }
            return total;
        }
    }
}
