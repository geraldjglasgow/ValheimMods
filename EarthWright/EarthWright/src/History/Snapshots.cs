using System;
using System.Collections.Generic;
using System.Linq;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// A named copy of the raw terrain values of a round area, kept in memory for this session: per heightmap the
    /// values packed and compressed (<see cref="RawPack"/>), unpacked only for a restore.
    /// </summary>
    internal sealed class Snapshot
    {
        public string Name;
        public Vector3 Center;
        public float Radius;
        public float Taken;
        public int Points;
        public readonly List<KeyValuePair<Vector3, byte[]>> Packed = new List<KeyValuePair<Vector3, byte[]>>();

        public int Bytes => Packed.Sum(part => part.Value.Length);

        /// <summary>The recorded values as a step, for <see cref="Restorer.Apply"/>.</summary>
        public Step Unpack()
        {
            Step values = new Step(new[] { Snapshots.SourcePrefix + Name }, EditFlags.None);
            foreach (KeyValuePair<Vector3, byte[]> part in Packed)
            {
                CompRecord record = new CompRecord { Position = part.Key };
                RawPack.Unpack(part.Value, record.Values);
                values.Add(record);
            }
            return values;
        }
    }

    /// <summary>
    /// Named area snapshots (<c>ew snapshot</c>): the raw compiler values of every vertex within a radius of the
    /// player, restored later through the same Restore edits as undo. A restore is itself a step on the undo list, so
    /// it can be taken back. Snapshots live only in memory, compressed and within <see cref="MaxBytes"/> together, and
    /// are dropped with the history at logout.
    /// </summary>
    internal static class Snapshots
    {
        public const string SourcePrefix = "snapshot:";
        public const int Max = 32;
        public const float MaxRadius = 128f;

        /// <summary>Memory all snapshots may take together (compressed; a 128 m snapshot of untouched ground takes about half a megabyte).</summary>
        public const int MaxBytes = 16 * 1024 * 1024;

        private static readonly Dictionary<string, Snapshot> saved = new Dictionary<string, Snapshot>(StringComparer.OrdinalIgnoreCase);

        public static IEnumerable<Snapshot> All => saved.Values.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase);

        public static int Count => saved.Count;

        public static bool Has(string name) => saved.ContainsKey(name);

        public static Snapshot Find(string name) => saved.TryGetValue(name, out Snapshot snapshot) ? snapshot : null;

        public static bool Delete(string name) => saved.Remove(name);

        public static void Clear() => saved.Clear();

        /// <summary>
        /// Records the area around the centre under the name (replacing a snapshot of that name). Partial: some of it is
        /// not loaded. Null when keeping it would pass <see cref="MaxBytes"/>; nothing is replaced then.
        /// </summary>
        public static Snapshot Save(string name, Vector3 center, float radius, out bool partial)
        {
            Snapshot snapshot = new Snapshot { Name = name, Center = center, Radius = radius, Taken = Time.time };
            List<Heightmap> maps = new List<Heightmap>();
            Heightmap.FindHeightmap(center, radius, maps);
            foreach (Heightmap map in maps)
                Record(snapshot, map, center, radius);
            partial = MissingGround(center, radius);
            int others = saved.Values.Where(s => !string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)).Sum(s => s.Bytes);
            if (others + snapshot.Bytes > MaxBytes)
                return null;
            saved[name] = snapshot;
            return snapshot;
        }

        /// <summary>Puts the snapshot back; what the ground held before goes on the undo list.</summary>
        public static RestoreOutcome Restore(Snapshot snapshot)
        {
            Timeline.Open = null;
            RestoreOutcome outcome = Restorer.Apply(snapshot.Unpack(), SourcePrefix + snapshot.Name);
            if (outcome.Sent > 0)
                Timeline.Record(outcome.Inverse);
            return outcome;
        }

        private static void Record(Snapshot snapshot, Heightmap map, Vector3 center, float radius)
        {
            if (RawAccess.MapAt(map.transform.position) != map)
                return;
            TerrainComp comp = RawAccess.CompOf(map);
            List<int> indices = AreaIndices.Circle(map, center, radius);
            if (indices.Count == 0)
                return;
            snapshot.Packed.Add(new KeyValuePair<Vector3, byte[]>(map.transform.position, RawPack.Pack(map, comp, indices)));
            snapshot.Points += indices.Count;
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
