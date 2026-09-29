using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DevBridge.Eval
{
    /// <summary>
    /// Types by full name and by short name. Short names prefer the game, then Unity, then plugins, then the rest,
    /// so Player, Console and Object mean the game's and Unity's types.
    /// </summary>
    internal static class TypeIndex
    {
        private static Dictionary<string, Type> byName;
        private static int assembliesIndexed;

        internal static Type Find(string name)
        {
            if (byName == null) Build();
            if (byName.TryGetValue(name, out Type type)) return type;
            if (AppDomain.CurrentDomain.GetAssemblies().Length == assembliesIndexed) return null;
            Build();
            return byName.TryGetValue(name, out type) ? type : null;
        }

        private static void Build()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var map = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (Assembly assembly in assemblies.OrderBy(Rank))
                foreach (Type type in TypesOf(assembly))
                    Add(map, type);
            byName = map;
            assembliesIndexed = assemblies.Length;
        }

        private static void Add(Dictionary<string, Type> map, Type type)
        {
            if (type.FullName == null || type.FullName.IndexOf('<') >= 0) return;
            string full = type.FullName.Replace('+', '.');
            if (!map.ContainsKey(full)) map[full] = type;
            if (!map.ContainsKey(type.Name)) map[type.Name] = type;
        }

        private static int Rank(Assembly assembly)
        {
            string name = assembly.GetName().Name;
            if (name == "assembly_valheim") return 0;
            if (name.StartsWith("assembly_") || name == "gui_framework") return 1;
            if (name.StartsWith("UnityEngine") || name.StartsWith("Unity.")) return 2;
            if (name.StartsWith("System") || name == "mscorlib" || name.StartsWith("Mono.")) return 4;
            return 3;
        }

        private static IEnumerable<Type> TypesOf(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException partial) { return partial.Types.Where(t => t != null); }
            catch (Exception) { return Enumerable.Empty<Type>(); }
        }
    }
}
