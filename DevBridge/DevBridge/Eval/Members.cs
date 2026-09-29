using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Eval
{
    /// <summary>Member lookup across the whole type chain, private members included.</summary>
    internal static class Members
    {
        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        internal static object Read(object target, Type type, string name, object[] args)
        {
            if (args != null) return Call(target, type, name, args);
            switch (Find(type, name, target == null))
            {
                case FieldInfo field: return field.GetValue(target);
                case PropertyInfo property: return property.GetValue(target, null);
                case Type nested: return new StaticRef(nested);
            }
            Component sibling = SiblingComponent(target, name);
            if (sibling) return sibling;
            throw new BridgeException($"{type.Name} has no {(target == null ? "static " : "")}field, property or component '{name}'");
        }

        internal static void Write(object target, Type type, string name, object value)
        {
            MemberInfo member = Find(type, name, target == null);
            if (member is FieldInfo field) field.SetValue(target, Coerce.To(value, field.FieldType));
            else if (member is PropertyInfo property && property.CanWrite) property.SetValue(target, Coerce.To(value, property.PropertyType), null);
            else throw new BridgeException($"{type.Name} has no writable '{name}'");
        }

        /// <summary>A field, property or (static only) nested type; an exact name first, then ignoring case.</summary>
        internal static MemberInfo Find(Type type, string name, bool isStatic)
        {
            return FindIn(type, name, isStatic, BindingFlags.Default) ?? FindIn(type, name, isStatic, BindingFlags.IgnoreCase);
        }

        private static MemberInfo FindIn(Type type, string name, bool isStatic, BindingFlags extra)
        {
            BindingFlags flags = Declared | extra | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            for (Type t = type; t != null; t = t.BaseType)
            {
                MemberInfo member = (MemberInfo)t.GetField(name, flags)
                    ?? (MemberInfo)t.GetProperties(flags).FirstOrDefault(p => p.GetIndexParameters().Length == 0 && Same(p.Name, name, extra))
                    ?? (isStatic ? t.GetNestedType(name, Declared | extra) : null);
                if (member != null) return member;
            }
            return null;
        }

        private static bool Same(string a, string b, BindingFlags extra) =>
            string.Equals(a, b, extra == BindingFlags.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        /// <summary>$hover.Character reaches the Character component of a GameObject or of a component's GameObject.</summary>
        private static Component SiblingComponent(object target, string name)
        {
            GameObject go = target as GameObject ?? (target as Component)?.gameObject;
            return go ? go.GetComponent(name) : null;
        }

        private static object Call(object target, Type type, string name, object[] args)
        {
            foreach (MethodInfo method in Methods(type, name, target == null))
            {
                object[] converted = Coerce.Args(method.GetParameters(), args);
                if (converted != null) return method.Invoke(target, converted);
            }
            throw new BridgeException($"{type.Name} has no {(target == null ? "static " : "")}method {name} that takes " +
                                      $"({string.Join(", ", args.Select(Describe.TypeName))})");
        }

        internal static IEnumerable<MethodInfo> Methods(Type type, string name, bool isStatic)
        {
            BindingFlags flags = Declared | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            for (Type t = type; t != null; t = t.BaseType)
                foreach (MethodInfo method in t.GetMethods(flags))
                    if (method.Name == name && !method.IsGenericMethodDefinition) yield return method;
        }
    }
}
