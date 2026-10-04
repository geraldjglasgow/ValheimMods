using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevBridge.Eval;
using DevBridge.Server;

namespace DevBridge.Trace
{
    /// <summary>
    /// The method a tracepoint names: Type.Method, Type.Method(int,string), Type..ctor, Type.get_Name. The type is found as
    /// /eval finds it (namespaced or short, nested as Outer.Inner); methods inherited from a base type count too, unless
    /// overridden. Several overloads need a parameter list or overload=N.
    /// </summary>
    internal static class MethodFinder
    {
        private const BindingFlags Declared =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        internal static MethodBase Find(string text, int overload)
        {
            int paren = text.IndexOf('(');
            string path = (paren < 0 ? text : text.Substring(0, paren)).Trim();
            string[] wanted = paren < 0 ? null : ParameterList.Split(text.Substring(paren), text);
            int dot = path.LastIndexOf('.');
            if (dot > 0 && path[dot - 1] == '.') dot--;
            if (dot <= 0 || dot == path.Length - 1) throw new BridgeException($"method= takes Type.Method, not '{text}'");
            string typeName = path.Substring(0, dot), name = path.Substring(dot + 1);
            Type type = FindType(typeName) ?? throw new BridgeException($"no type '{typeName}' (give its full name when it has a namespace)");
            if (name == "ctor" || name == "new") name = ".ctor";
            List<MethodBase> candidates = Candidates(type, name);
            if (candidates.Count == 0) throw NotFound(type, name);
            if (wanted != null) candidates = candidates.Where(method => ParameterList.Matches(method, wanted)).ToList();
            return Pick(candidates, overload, type, name, text);
        }

        /// <summary>A type by full or short name, or a nested type under one (Outer.Inner) when the index has no such name.</summary>
        private static Type FindType(string name)
        {
            Type type = TypeIndex.Find(name);
            if (type != null) return type;
            int dot = name.LastIndexOf('.');
            if (dot <= 0) return null;
            Type outer = FindType(name.Substring(0, dot));
            return outer?.GetNestedType(name.Substring(dot + 1), BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>The methods of that name on the type and its bases, an override or a hiding method replacing the base's.</summary>
        private static List<MethodBase> Candidates(Type type, string name)
        {
            if (name == ".ctor" || name == ".cctor")
                return type.GetConstructors(Declared).Where(ctor => ctor.Name == name).Cast<MethodBase>().ToList();
            var found = new List<MethodBase>();
            for (Type t = type; t != null; t = t.BaseType)
            {
                IEnumerable<MethodInfo> named = t.GetMethods(Declared).Where(m => m.Name == name);
                foreach (MethodInfo method in named.OrderBy(TraceText.Signature, StringComparer.Ordinal))
                    if (!found.Any(known => SameParameters(known, method))) found.Add(method);
            }
            return found;
        }

        private static bool SameParameters(MethodBase a, MethodBase b) =>
            a.GetParameters().Select(p => p.ParameterType).SequenceEqual(b.GetParameters().Select(p => p.ParameterType));

        private static MethodBase Pick(List<MethodBase> candidates, int overload, Type type, string name, string text)
        {
            if (overload >= 0 && overload < candidates.Count) return candidates[overload];
            if (candidates.Count == 1 && overload < 0) return candidates[0];
            string list = string.Join("\n", candidates.Select((method, i) => $"  [{i}] {TraceText.Signature(method)}"));
            if (candidates.Count == 0)
                throw new BridgeException($"no overload of {TraceText.Path(type)}.{name} matches {text}; they are:\n" + Overloads(type, name));
            if (overload >= 0) throw new BridgeException($"overload={overload} is out of range:\n{list}");
            throw new BridgeException($"{TraceText.Path(type)}.{name} has {candidates.Count} overloads; name one with its " +
                                      $"parameter types, {TraceText.Path(type)}.{name}(int,string), or with overload=N:\n{list}");
        }

        private static string Overloads(Type type, string name) =>
            string.Join("\n", Candidates(type, name).Select(method => "  " + TraceText.Signature(method)));

        /// <summary>No such method: says when it is a property (trace get_X or set_X) and lists methods with similar names.</summary>
        private static BridgeException NotFound(Type type, string name)
        {
            string where = TraceText.Path(type);
            if (type.GetProperties(Declared & ~BindingFlags.DeclaredOnly).Any(property => property.Name == name))
                return new BridgeException($"{where}.{name} is a property: trace {where}.get_{name} or {where}.set_{name}");
            List<string> similar = Similar(type, name);
            string hint = similar.Count == 0 ? "" : "; similar: " + string.Join(", ", similar);
            return new BridgeException($"{where} has no method '{name}' (constructors are {where}..ctor){hint}");
        }

        private static List<string> Similar(Type type, string name)
        {
            var names = new SortedSet<string>(StringComparer.Ordinal);
            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (MethodInfo method in t.GetMethods(Declared))
                    if (method.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) names.Add(method.Name);
            return names.Take(12).ToList();
        }
    }
}
