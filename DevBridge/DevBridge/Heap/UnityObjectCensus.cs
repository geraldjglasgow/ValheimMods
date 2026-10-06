using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DevBridge.Server;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace DevBridge.Heap
{
    /// <summary>
    /// /heap?objects=1: every loaded Unity object (scene objects, assets, hidden ones) counted by type with its native
    /// size (Profiler.GetRuntimeMemorySizeLong, which this release build does report). type= lists that type's largest
    /// objects by name instead. Walking them all is a hitch of its own, reported as census_ms.
    /// </summary>
    internal static class UnityObjectCensus
    {
        private static readonly string[] Columns = { "count", "kb" };

        internal static Dictionary<string, object> Reply(BridgeRequest request)
        {
            Stopwatch watch = Stopwatch.StartNew();
            Object[] all = Resources.FindObjectsOfTypeAll<Object>();
            int top = Mathf.Clamp(request.Int("top", 25), 1, 1000);
            string type = request.Get("type");
            Dictionary<string, object> reply = type != null ? Largest(all, type, top) : Tally(all, request, top);
            reply["census_ms"] = watch.ElapsedMilliseconds;
            return reply;
        }

        private static Dictionary<string, object> Tally(Object[] all, BridgeRequest request, int top)
        {
            Dictionary<string, long[]> rows = Rows(all);
            var reply = new Dictionary<string, object>
            {
                ["objects"] = all.Length,
                ["native_mb"] = Fmt.R(rows.Values.Sum(r => r[1]) / 1024f),
            };
            string mark = request.Get("mark"), diff = request.Get("diff");
            if (diff != null) reply["changes"] = HeapMarks.Diff("objects", diff, rows, Columns, r => r[1] != 0 ? r[1] : r[0], top);
            else reply["types"] = Top(rows, request.Get("sort") == "count" ? 0 : 1, top);
            if (mark != null) HeapMarks.Save("objects", mark, rows);
            return reply;
        }

        private static Dictionary<string, long[]> Rows(Object[] all)
        {
            var rows = new Dictionary<string, long[]>();
            foreach (Object item in all)
            {
                if (item == null) continue;
                string name = item.GetType().Name;
                if (!rows.TryGetValue(name, out long[] row)) rows[name] = row = new long[2];
                row[0]++;
                row[1] += Profiler.GetRuntimeMemorySizeLong(item) / 1024;
            }
            return rows;
        }

        private static List<Dictionary<string, object>> Top(Dictionary<string, long[]> rows, int column, int top) =>
            rows.OrderByDescending(r => r.Value[column]).Take(top).Select(r => new Dictionary<string, object>
            {
                ["type"] = r.Key,
                ["count"] = r.Value[0],
                ["mb"] = Fmt.R(r.Value[1] / 1024f),
            }).ToList();

        private static Dictionary<string, object> Largest(Object[] all, string type, int top)
        {
            List<Object> matching = all.Where(o => o != null && string.Equals(o.GetType().Name, type, StringComparison.OrdinalIgnoreCase)).ToList();
            return new Dictionary<string, object>
            {
                ["type"] = type,
                ["count"] = matching.Count,
                ["largest"] = matching.Select(o => (o, size: Profiler.GetRuntimeMemorySizeLong(o)))
                    .OrderByDescending(p => p.size).Take(top)
                    .Select(p => Describe(p.o, p.size)).ToList(),
            };
        }

        private static Dictionary<string, object> Describe(Object item, long size)
        {
            var row = new Dictionary<string, object> { ["name"] = item.name, ["kb"] = size / 1024 };
            if (item is Texture texture) row["size"] = $"{texture.width}x{texture.height}";
            if (item is Mesh mesh) row["vertices"] = mesh.vertexCount;
            if (item is GameObject go) row["active"] = go.activeInHierarchy;
            return row;
        }
    }
}
