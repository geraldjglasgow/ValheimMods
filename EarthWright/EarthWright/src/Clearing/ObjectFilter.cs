using System;
using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// The include= and ignore= filters of <c>ew terrain</c>: the loaded objects whose prefab name matches (case ignored,
    /// each * matches any run of characters), taken with the flat bounds of their colliders. A vertex is kept
    /// when it lies within <see cref="Margin"/> of an included object (if any are named) and of no ignored one.
    /// </summary>
    public sealed class ObjectFilter
    {
        private const float Margin = 1f;
        private const float SearchExtra = 20f;

        private readonly List<Bounds> included = new List<Bounds>();
        private readonly List<Bounds> ignored = new List<Bounds>();
        private bool anyInclude;

        /// <summary>The objects named by the request within <paramref name="reach"/> (plus room for big objects) of the centre.</summary>
        public static ObjectFilter For(TerrainRequest request, Vector3 center, float reach)
        {
            ObjectFilter filter = new ObjectFilter { anyInclude = request.Include.Count > 0 };
            foreach (ZNetView view in ObjectScan.Within(center, reach + SearchExtra, null))
            {
                string name = Utils.GetPrefabName(view.gameObject);
                if (Matches(request.Include, name))
                    filter.included.Add(FlatBounds(view.gameObject));
                if (Matches(request.Ignore, name))
                    filter.ignored.Add(FlatBounds(view.gameObject));
            }
            return filter;
        }

        public bool Keeps(float x, float z) => (!anyInclude || Near(included, x, z)) && !Near(ignored, x, z);

        private static bool Near(List<Bounds> objects, float x, float z)
        {
            foreach (Bounds b in objects)
            {
                float dx = Mathf.Max(b.min.x - x, 0f, x - b.max.x);
                float dz = Mathf.Max(b.min.z - z, 0f, z - b.max.z);
                if (dx * dx + dz * dz <= Margin * Margin)
                    return true;
            }
            return false;
        }

        /// <summary>The union of the object's solid colliders, or its position when it has none.</summary>
        private static Bounds FlatBounds(GameObject go)
        {
            Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
            bool any = false;
            foreach (Collider collider in go.GetComponentsInChildren<Collider>())
            {
                if (collider == null || collider.isTrigger || !collider.enabled)
                    continue;
                if (any)
                    bounds.Encapsulate(collider.bounds);
                else
                    bounds = collider.bounds;
                any = true;
            }
            return bounds;
        }

        public static bool Matches(IEnumerable<string> patterns, string name)
        {
            foreach (string pattern in patterns)
            {
                if (Matches(pattern, name))
                    return true;
            }
            return false;
        }

        /// <summary>A name pattern where each * stands for any run of characters (case ignored).</summary>
        private static bool Matches(string pattern, string name)
        {
            const StringComparison Ignore = StringComparison.OrdinalIgnoreCase;
            string[] parts = pattern.Split('*');
            if (parts.Length == 1)
                return string.Equals(pattern, name, Ignore);
            string first = parts[0], last = parts[parts.Length - 1];
            if (name.Length < first.Length + last.Length || !name.StartsWith(first, Ignore) || !name.EndsWith(last, Ignore))
                return false;
            int pos = first.Length, end = name.Length - last.Length;
            for (int i = 1; i < parts.Length - 1; i++)
            {
                int found = name.IndexOf(parts[i], pos, end - pos, Ignore);
                if (found < 0)
                    return false;
                pos = found + parts[i].Length;
            }
            return true;
        }
    }
}
