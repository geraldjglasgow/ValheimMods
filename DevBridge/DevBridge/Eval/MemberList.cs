using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DevBridge.Eval
{
    /// <summary>Lists a type's fields and properties with their values, or its method signatures, up to Unity's base classes.</summary>
    internal static class MemberList
    {
        private static readonly HashSet<Type> Stop = new HashSet<Type>
        {
            typeof(object), typeof(UnityEngine.Object), typeof(Component), typeof(Behaviour), typeof(MonoBehaviour), typeof(ValueType),
        };

        // Unity getters that silently create a copy of the asset on every read
        private static readonly HashSet<string> Copying = new HashSet<string> { "material", "materials", "mesh" };

        internal static void Values(StringBuilder text, object target, Type type, string filter)
        {
            text.AppendLine(target == null ? "static fields and properties:" : "fields and properties:");
            foreach (Type t in Chain(type))
                foreach (MemberInfo member in t.GetMembers(Flags(target == null)))
                    if (IsValue(member) && Matches(member.Name, filter))
                        text.Append("  ").AppendLine(ValueEntry(member, target));
        }

        internal static void Methods(StringBuilder text, Type type, bool isStatic, string filter)
        {
            text.AppendLine(isStatic ? "static methods:" : "methods:");
            foreach (Type t in Chain(type))
                foreach (MethodInfo method in t.GetMethods(Flags(isStatic)))
                    if (!method.IsSpecialName && Matches(method.Name, filter))
                        text.Append("  ").AppendLine(Signature(method));
        }

        private static IEnumerable<Type> Chain(Type type)
        {
            for (Type t = type; t != null && (!Stop.Contains(t) || t == type); t = t.BaseType) yield return t;
        }

        private static BindingFlags Flags(bool isStatic) =>
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | (isStatic ? BindingFlags.Static : BindingFlags.Instance);

        private static bool IsValue(MemberInfo member) =>
            member is FieldInfo field && field.Name.IndexOf('<') < 0
            || member is PropertyInfo property && property.CanRead && property.GetIndexParameters().Length == 0;

        private static bool Matches(string name, string filter) =>
            filter == null || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        private static string ValueEntry(MemberInfo member, object target)
        {
            Type type = member is FieldInfo f ? f.FieldType : ((PropertyInfo)member).PropertyType;
            if (member is PropertyInfo && Copying.Contains(member.Name)) return $"{member.Name}: {Describe.TypeName(type)} (not read: it copies)";
            string value;
            try
            {
                object read = member is FieldInfo field ? field.GetValue(target) : ((PropertyInfo)member).GetValue(target, null);
                value = Describe.Value(read);
            }
            catch (Exception error)
            {
                value = "<threw " + (error.InnerException ?? error).GetType().Name + ">";
            }
            return $"{member.Name}: {Describe.TypeName(type)} = {value}";
        }

        private static string Signature(MethodInfo method)
        {
            string parameters = string.Join(", ", method.GetParameters().Select(p => Describe.TypeName(p.ParameterType) + " " + p.Name));
            return $"{method.Name}({parameters}): {Describe.TypeName(method.ReturnType)}";
        }
    }
}
