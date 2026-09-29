using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Eval
{
    /// <summary>Converts evaluated values to parameter and member types: numbers, enums by name, "x,y,z" vectors.</summary>
    internal static class Coerce
    {
        /// <summary>The arguments converted for these parameters, defaults filled in, or null when they do not fit.</summary>
        internal static object[] Args(ParameterInfo[] parameters, object[] args)
        {
            if (args.Length > parameters.Length) return null;
            var result = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < args.Length && TryTo(args[i], parameters[i].ParameterType, out result[i])) continue;
                if (i < args.Length || !parameters[i].IsOptional) return null;
                result[i] = parameters[i].DefaultValue;
            }
            return result;
        }

        internal static object To(object value, Type type) =>
            TryTo(value, type, out object result) ? result : throw new BridgeException($"cannot use {Describe.Value(value)} as {type.Name}");

        internal static bool TryTo(object value, Type type, out object result)
        {
            result = null;
            if (type.IsByRef) type = type.GetElementType();
            if (value == null) return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
            if (type.IsInstanceOfType(value)) { result = value; return true; }
            try { result = Convert(value, Nullable.GetUnderlyingType(type) ?? type); }
            catch (Exception) { result = null; }
            return result != null;
        }

        private static object Convert(object value, Type type)
        {
            if (type.IsEnum) return value is string name ? Enum.Parse(type, name, true) : Enum.ToObject(type, value);
            if (value is string text) return FromText(text, type);
            if (type == typeof(string) || !(value is IConvertible) || !typeof(IConvertible).IsAssignableFrom(type)) return null;
            return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        private static object FromText(string text, Type type)
        {
            if (type == typeof(Vector3)) return Fmt.ParseV3(text, "a Vector3");
            if (type == typeof(Vector2)) { float[] n = Fmt.Numbers(text, 2, "a Vector2"); return new Vector2(n[0], n[1]); }
            if (type == typeof(char) && text.Length == 1) return text[0];
            return null;
        }
    }

    /// <summary>target[key] for arrays, lists, dictionaries and any other sequence (by position).</summary>
    internal static class Indexing
    {
        internal static object Get(object target, object key)
        {
            if (target == null) throw new BridgeException("cannot index null");
            if (target is Array array) return array.GetValue(Position(key));
            PropertyInfo indexer = Indexer(target.GetType());
            if (indexer != null) return indexer.GetValue(target, new[] { Coerce.To(key, indexer.GetIndexParameters()[0].ParameterType) });
            if (target is IEnumerable items) return items.Cast<object>().ElementAt(Position(key));
            throw new BridgeException($"{target.GetType().Name} cannot be indexed");
        }

        internal static void Set(object target, object key, object value)
        {
            if (target is Array array) { array.SetValue(Coerce.To(value, array.GetType().GetElementType()), Position(key)); return; }
            PropertyInfo indexer = target == null ? null : Indexer(target.GetType());
            if (indexer == null || !indexer.CanWrite) throw new BridgeException("that element cannot be assigned");
            object typedKey = Coerce.To(key, indexer.GetIndexParameters()[0].ParameterType);
            indexer.SetValue(target, Coerce.To(value, indexer.PropertyType), new[] { typedKey });
        }

        private static int Position(object key) => (int)Coerce.To(key, typeof(int));

        private static PropertyInfo Indexer(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.GetIndexParameters().Length == 1);
    }
}
