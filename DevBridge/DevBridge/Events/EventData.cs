using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevBridge.Events
{
    /// <summary>
    /// Turns what a publisher hands in into plain data (strings, numbers, lists, maps) that any thread may read and
    /// serialise: vectors become number arrays, enums their names, Unity objects their names, anything else its text.
    /// </summary>
    internal static class EventData
    {
        private const int Depth = 4;

        internal static Dictionary<string, object> Plain(Dictionary<string, object> data)
        {
            var plain = new Dictionary<string, object>();
            if (data == null) return plain;
            foreach (KeyValuePair<string, object> pair in data) plain[pair.Key] = Value(pair.Value, Depth);
            return plain;
        }

        private static object Value(object value, int depth)
        {
            switch (value)
            {
                case null: return null;
                case string _: return value;
                case Enum _: return value.ToString();
                case Vector3 v: return Fmt.V3(v);
                case Vector2 v: return new[] { Fmt.R(v.x), Fmt.R(v.y) };
                case Quaternion q: return new[] { Fmt.R(q.x), Fmt.R(q.y), Fmt.R(q.z), Fmt.R(q.w) };
                case UnityEngine.Object o: return ObjectName(o);
                case IDictionary map: return depth > 0 ? Map(map, depth - 1) : map.ToString();
                case IEnumerable list: return depth > 0 ? List(list, depth - 1) : list.ToString();
            }
            return value.GetType().IsPrimitive || value is decimal ? value : value.ToString();
        }

        // A Unity object's name may only be read on the main thread; elsewhere its type has to do.
        private static string ObjectName(UnityEngine.Object o) =>
            !EventLog.OnMainThread ? o.GetType().Name : o ? o.name : "(destroyed)";

        private static Dictionary<string, object> Map(IDictionary map, int depth)
        {
            var plain = new Dictionary<string, object>();
            foreach (DictionaryEntry entry in map) plain[entry.Key?.ToString() ?? ""] = Value(entry.Value, depth);
            return plain;
        }

        private static List<object> List(IEnumerable list, int depth)
        {
            var plain = new List<object>();
            foreach (object item in list) plain.Add(Value(item, depth));
            return plain;
        }
    }
}
