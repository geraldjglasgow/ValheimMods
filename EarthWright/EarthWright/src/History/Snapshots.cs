using System;
using System.Collections.Generic;
using System.Linq;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>A named copy of the raw terrain values of a round area, kept in memory for this session.</summary>
    internal sealed class Snapshot
    {
        public string Name;
        public Vector3 Center;
        public float Radius;
        public float Taken;
        public Step Values;
    }

    /// <summary>
    /// Named area snapshots (<c>ew snapshot</c>): the raw compiler values of every vertex within a radius of the
    /// player, restored later through the same Restore edits as undo. A restore is itself a step on the undo list, so
    /// it can be taken back. Snapshots live only in memory and are dropped with the history at logout.
    /// </summary>
    internal static class Snapshots
    {
        public const string SourcePrefix = "snapshot:";
        public const int Max = 32;
        public const float MaxRadius = 128f;

        private static readonly Dictionary<string, Snapshot> saved = new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase);

        public static IEnumerable<Snapshot> All => saved.Values.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase);

        public static int Count => saved.Count;

        public static bool Has(string name) => saved.ContainsKey(name);

        public static Snapshot Find(string name) => saved.TryGetValue(name, out Snapshot snapshot) ? snapshot : null;

        public static bool Delete(string name) => saved.Remove(name);

        public static void Clear() => saved.Clear();

        /// <summary>Records the area around the centre under the name (replacing a snapshot of that name). Partial: some of it is not loaded.</summary>
        public static Snapshot Save(string name, Vector3 center, float radius, out bool partial)
        {
            Step values = new Step(new[] { SourcePrefix + name }, EditFlags.None);
            List<Heightmap> maps = new List<Heightmap>();
            Heightmap.FindHeightmap(center, radius, maps);
            foreach (Heightmap map in maps)
                Record(values, map, center, radius);
            partial = MissingGround(center, radius);
            Snapshot snapshot = new Snapshot { Name = name, Center = center, Radius = radius, Taken = Time.time, Values = values };
            saved[name] = snapshot;
            return snapshot;
        }

        /// <summary>Puts the snapshot back; what the ground held before goes on the undo list.</summary>
        public static RestoreOutcome Restore(Snapshot snapshot)
        {
            Timeline.Open = null;
            RestoreOutcome outcome = Restorer.Apply(snapshot.Values, SourcePrefix + snapshot.Name);
            if (outcome.Sent > 0)
                Timeline.Record(outcome.Inverse);
            return outcome;
        }

        private static void Record(Step values, Heightmap map, Vector3 center, float radius)
        {
            if (RawAccess.MapAt(map.transform.position) != map)
                return;
            TerrainComp comp = RawAccess.CompOf(map);
            CompRecord record = values.For(map);
            foreach (int index in AreaIndices.Circle(map, center, radius))
                record.Values[index] = RawAccess.Read(map, comp, index);
        }

        /// <summary>Some ground within the radius has no loaded heightmap here (sampled every 16 m).</summary>
        private static bool MissingGround(Vector3 center, float radius)
        {
            for (float x = -radius; x <= radius; x += 16f)
            {
                for (float z = -radius; z <= radius; z += 16f)
                {
                    if (x * x + z * z <= radius * radius && RawAccess.MapAt(center + new Vector3(x, 0f, z)) == null)
                        return true;
                }
            }
            return false;
        }
    }
}
