using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DevBridge.Reload
{
    /// <summary>Whether a method or delegate belongs to an assembly, and multicast delegates with its handlers taken out.</summary>
    internal static class OldCode
    {
        internal static bool In(MethodBase method, Assembly assembly) => method?.DeclaringType?.Assembly == assembly;

        /// <summary>
        /// True when any handler of the delegate is the assembly's code, or a closure of another assembly that holds a
        /// delegate of it (a wrapper lambda keeps the real handler in a field of its closure).
        /// </summary>
        internal static bool Owns(Delegate value, Assembly assembly) => Owns(value, assembly, 2);

        private static bool Owns(Delegate value, Assembly assembly, int depth)
        {
            if (value == null) return false;
            foreach (Delegate single in value.GetInvocationList())
            {
                if (In(single.Method, assembly) || single.Target?.GetType().Assembly == assembly) return true;
                if (depth > 0 && ClosureOwns(single.Target, assembly, depth - 1)) return true;
            }
            return false;
        }

        private static bool ClosureOwns(object closure, Assembly assembly, int depth)
        {
            if (closure == null || !closure.GetType().IsDefined(typeof(CompilerGeneratedAttribute), false)) return false;
            return DelegateFields(closure.GetType()).Any(f => Owns(f.GetValue(closure) as Delegate, assembly, depth));
        }

        /// <summary>The delegate with the assembly's handlers removed; null when none is left.</summary>
        internal static Delegate Without(Delegate value, Assembly assembly, out int removed)
        {
            removed = 0;
            if (value == null) return null;
            var kept = new List<Delegate>();
            foreach (Delegate single in value.GetInvocationList())
            {
                if (Owns(single, assembly)) removed++;
                else kept.Add(single);
            }
            return removed == 0 ? value : Delegate.Combine(kept.ToArray());
        }

        /// <summary>The first of the delegate's handlers that belongs to the assembly, as Type.Method.</summary>
        internal static string Name(Delegate value, Assembly assembly)
        {
            Delegate single = value?.GetInvocationList().FirstOrDefault(d => Owns(d, assembly));
            return single == null ? "?" : $"{single.Method.DeclaringType?.Name}.{single.Method.Name}";
        }

        private static readonly Dictionary<Type, FieldInfo[]> Fields = new Dictionary<Type, FieldInfo[]>();

        /// <summary>Instance fields of delegate type, cached per type (RPC method wrappers, closures).</summary>
        internal static FieldInfo[] DelegateFields(Type type)
        {
            if (Fields.TryGetValue(type, out FieldInfo[] fields)) return fields;
            fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => typeof(Delegate).IsAssignableFrom(f.FieldType)).ToArray();
            Fields[type] = fields;
            return fields;
        }

        /// <summary>True when one of the object's delegate fields is the assembly's code (a registered RPC handler).</summary>
        internal static bool Holds(object owner, Assembly assembly) =>
            owner != null && DelegateFields(owner.GetType()).Any(f => Owns(f.GetValue(owner) as Delegate, assembly));

        /// <summary>The handler a wrapper object holds, as Type.Method.</summary>
        internal static string HeldName(object owner, Assembly assembly)
        {
            Delegate held = DelegateFields(owner.GetType()).Select(f => f.GetValue(owner) as Delegate).FirstOrDefault(d => Owns(d, assembly));
            return Name(held, assembly);
        }
    }
}
