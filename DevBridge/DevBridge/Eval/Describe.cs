using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DevBridge.Eval
{
    /// <summary>Text for evaluated values: "Type = value", sequences item by item.</summary>
    internal static class Describe
    {
        private const int MaxItems = 50;

        internal static string Full(object value, bool members, bool methods, string filter)
        {
            var text = new StringBuilder(Line(value)).AppendLine();
            if (value is IEnumerable sequence && !(value is string)) Items(text, sequence);
            if (value == null) return text.ToString();
            Type type = value is StaticRef reference ? reference.Type : value.GetType();
            object target = value is StaticRef ? null : value;
            if (members) MemberList.Values(text, target, type, filter);
            if (methods) MemberList.Methods(text, type, target == null, filter);
            return text.ToString();
        }

        internal static string Line(object value) => value == null ? "null" : $"{TypeName(value)} = {Value(value)}";

        internal static string Value(object value)
        {
            switch (value)
            {
                case null: return "null";
                case string text: return "\"" + Fmt.Clip(text, 400) + "\"";
                case UnityEngine.Object unity when !unity: return "null (destroyed)";
                case StaticRef reference: return "type " + reference.Type.FullName;
                case float number: return number.ToString("R", CultureInfo.InvariantCulture);
                case double number: return number.ToString("R", CultureInfo.InvariantCulture);
                case Vector3 vector: return vector.ToString("F3");
                case Quaternion rotation: return rotation.eulerAngles.ToString("F1") + " euler";
                case GameObject go: return $"{go.name} (GameObject{(go.activeInHierarchy ? "" : ", inactive")})";
                case Component component: return $"{component.name} ({component.GetType().Name})";
                case IEnumerable sequence: return "count " + Count(sequence);
                default: return Fmt.Clip(value.ToString(), 400);
            }
        }

        internal static string TypeName(object value) =>
            value is StaticRef ? "static" : value == null ? "null" : TypeName(value.GetType());

        internal static string TypeName(Type type)
        {
            if (!type.IsGenericType) return type.Name;
            int tick = type.Name.IndexOf('`');
            string name = tick > 0 ? type.Name.Substring(0, tick) : type.Name;
            return name + "<" + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">";
        }

        private static void Items(StringBuilder text, IEnumerable sequence)
        {
            int index = 0;
            foreach (object item in sequence)
            {
                if (index == MaxItems) { text.AppendLine($"  ... more (index with [n] to reach them)"); return; }
                text.Append("  [").Append(index++).Append("] ").AppendLine(Entry(item));
            }
        }

        private static string Entry(object item)
        {
            Type type = item?.GetType();
            if (type == null || !type.IsGenericType || type.GetGenericTypeDefinition() != typeof(System.Collections.Generic.KeyValuePair<,>))
                return Line(item);
            object key = type.GetProperty("Key").GetValue(item, null), value = type.GetProperty("Value").GetValue(item, null);
            return $"{Value(key)} => {Line(value)}";
        }

        private static string Count(IEnumerable sequence)
        {
            PropertyInfo count = sequence.GetType().GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
            if (count != null && count.GetIndexParameters().Length == 0) return count.GetValue(sequence, null).ToString();
            return sequence is Array array ? array.Length.ToString() : "?";
        }
    }
}
