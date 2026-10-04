using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevBridge.Eval;
using UnityEngine;

namespace DevBridge.Trace
{
    /// <summary>Short text for what a trace records: values, instances, method signatures in C# names.</summary>
    internal static class TraceText
    {
        internal const int Width = 120;

        private static readonly Dictionary<Type, string> Aliases = new Dictionary<Type, string>
        {
            [typeof(void)] = "void", [typeof(bool)] = "bool", [typeof(byte)] = "byte", [typeof(sbyte)] = "sbyte",
            [typeof(char)] = "char", [typeof(short)] = "short", [typeof(ushort)] = "ushort", [typeof(int)] = "int",
            [typeof(uint)] = "uint", [typeof(long)] = "long", [typeof(ulong)] = "ulong", [typeof(float)] = "float",
            [typeof(double)] = "double", [typeof(decimal)] = "decimal", [typeof(string)] = "string", [typeof(object)] = "object",
        };

        /// <summary>One value in at most Width characters. Unity objects are only read on the main thread.</summary>
        internal static string Brief(object value, bool mainThread)
        {
            try
            {
                switch (value)
                {
                    case null: return "null";
                    case UnityEngine.Object unity when mainThread: return Fmt.Clip(Unity(unity), Width);
                    case UnityEngine.Object _: return Describe.TypeName(value) + " (off the main thread)";
                    case HitData hit when mainThread: return Fmt.Clip(Hit(hit), Width);
                    default: return Fmt.Clip(Describe.Value(value), Width);
                }
            }
            catch (Exception error)
            {
                return $"{Describe.TypeName(value)} (reading it threw {error.GetType().Name})";
            }
        }

        /// <summary>A Character by its name and level; anything else on a GameObject by object name, type and prefab.</summary>
        private static string Unity(UnityEngine.Object unity)
        {
            if (!unity) return "null (destroyed)";
            if (unity is Character character) return Creature(character);
            GameObject go = unity as GameObject ?? (unity as Component)?.gameObject;
            string kind = Describe.TypeName(unity.GetType());
            if (go == null) return $"{unity.name} ({kind})";
            string prefab = Prefab(go);
            return prefab == null ? $"{go.name} ({kind})" : $"{go.name} ({kind}, prefab {prefab})";
        }

        private static string Creature(Character character)
        {
            string name = character is Player player ? "\"" + player.GetPlayerName() + "\"" : character.m_name;
            return $"{character.gameObject.name} ({character.GetType().Name} {name}, level {character.GetLevel()})";
        }

        /// <summary>The prefab named by the object's ZDO, when the object's own name does not show it (MineRock5 renames itself).</summary>
        private static string Prefab(GameObject go)
        {
            ZNetView view = go.GetComponent<ZNetView>();
            ZDO zdo = view ? view.GetZDO() : null;
            GameObject prefab = zdo != null && ZNetScene.instance ? ZNetScene.instance.GetPrefab(zdo.GetPrefab()) : null;
            if (!prefab || go.name == prefab.name || go.name.StartsWith(prefab.name + "(")) return null;
            return prefab.name;
        }

        private static string Hit(HitData hit)
        {
            Character attacker = hit.GetAttacker();
            string from = attacker ? " from " + attacker.gameObject.name : "";
            return FormattableString.Invariant($"HitData {Fmt.R(hit.GetTotalDamage())} damage ({hit.m_damage.ToString().Trim()}){from}");
        }

        /// <summary>Type.Method(int a, ref float b): result, as the overload list shows it and as method= accepts it.</summary>
        internal static string Signature(MethodBase method)
        {
            string parameters = string.Join(", ", method.GetParameters().Select(Parameter));
            string result = method is MethodInfo info ? ": " + CsName(info.ReturnType) : "";
            return $"{Path(method.DeclaringType)}.{method.Name}({parameters}){result}";
        }

        private static string Parameter(ParameterInfo parameter)
        {
            Type type = parameter.ParameterType;
            if (!type.IsByRef) return CsName(type) + " " + parameter.Name;
            string kind = parameter.IsOut ? "out " : parameter.IsIn ? "in " : "ref ";
            return kind + CsName(type.GetElementType()) + " " + parameter.Name;
        }

        /// <summary>A type as C# writes it: int, string[], List&lt;int&gt;; nested types without their outer type.</summary>
        internal static string CsName(Type type)
        {
            if (type.IsByRef) return CsName(type.GetElementType()) + "&";
            if (type.IsArray) return CsName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            if (Aliases.TryGetValue(type, out string alias)) return alias;
            if (!type.IsGenericType) return type.Name;
            int tick = type.Name.IndexOf('`');
            string name = tick > 0 ? type.Name.Substring(0, tick) : type.Name;
            return name + "<" + string.Join(", ", type.GetGenericArguments().Select(CsName)) + ">";
        }

        /// <summary>A type with its outer types (Outer.Inner), without a namespace.</summary>
        internal static string Path(Type type) =>
            type == null ? "?" : type.DeclaringType == null ? type.Name : Path(type.DeclaringType) + "." + type.Name;
    }
}
