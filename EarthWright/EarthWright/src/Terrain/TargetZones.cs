using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The target vertices of an edit sorted by the 64 m zone heightmap they lie on, in one pass over the targets (a
    /// vertex on a shared edge goes to both zones, as <c>Heightmap.IsPointInside</c> counts it for both). The dispatcher
    /// finds the touched heightmaps and each compiler's part from it, instead of testing every target against every
    /// loaded heightmap and every compiler (a long road or a 128 m <c>ew terrain</c> has tens of thousands of targets).
    /// The split is kept for the set being sent and forgotten when the send ends. A heightmap that is not a zone
    /// heightmap (none in the game) is tested target by target.
    /// </summary>
    internal static class TargetZones
    {
        private const int Size = 64;
        private const int Half = 32;

        private static readonly Dictionary<long, VertexSet> parts = new Dictionary<long, VertexSet>();
        private static VertexSet splitOf;

        /// <summary>The loaded heightmaps a target vertex lies on.</summary>
        public static void Maps(VertexSet set, List<Heightmap> maps)
        {
            Split(set);
            foreach (Heightmap map in Heightmap.GetAllHeightmaps())
            {
                if (map != null && (ZoneSized(map) ? parts.ContainsKey(KeyOf(map)) : Inside(map, set) != null))
                    maps.Add(map);
            }
        }

        /// <summary>The targets that lie on the heightmap, or null when none does.</summary>
        public static VertexSet PartOf(Heightmap map, VertexSet set)
        {
            if (!ZoneSized(map))
                return Inside(map, set);
            Split(set);
            return parts.TryGetValue(KeyOf(map), out VertexSet part) ? part : null;
        }

        /// <summary>The send is over: the next set is split again.</summary>
        public static void Forget()
        {
            splitOf = null;
            parts.Clear();
        }

        private static void Split(VertexSet set)
        {
            if (ReferenceEquals(set, splitOf))
                return;
            splitOf = set;
            parts.Clear();
            foreach (TargetVertex v in set.Targets)
            {
                int x = ZoneOf(v.X), z = ZoneOf(v.Z);
                bool xEdge = OnEdge(v.X), zEdge = OnEdge(v.Z);
                Add(x, z, v);
                if (xEdge)
                    Add(x - 1, z, v);
                if (zEdge)
                    Add(x, z - 1, v);
                if (xEdge && zEdge)
                    Add(x - 1, z - 1, v);
            }
        }

        private static void Add(int x, int z, TargetVertex v)
        {
            long key = Key(x, z);
            if (!parts.TryGetValue(key, out VertexSet part))
                parts[key] = part = new VertexSet { Mode = VertexMode.Targets };
            part.Targets.Add(v);
        }

        /// <summary>The targets inside a heightmap of any size, the slow way (null when none).</summary>
        private static VertexSet Inside(Heightmap map, VertexSet set)
        {
            VertexSet part = null;
            foreach (TargetVertex v in set.Targets)
            {
                if (!map.IsPointInside(new Vector3(v.X, 0f, v.Z)))
                    continue;
                if (part == null)
                    part = new VertexSet { Mode = VertexMode.Targets };
                part.Targets.Add(v);
            }
            return part;
        }

        /// <summary>The zone whose heightmap starts at or before the coordinate (its left or lower edge is c = zone * 64 - 32).</summary>
        private static int ZoneOf(int c) => Mathf.FloorToInt((c + Half) / (float)Size);

        private static bool OnEdge(int c) => ((c + Half) % Size + Size) % Size == 0;

        private static bool ZoneSized(Heightmap map)
        {
            if (Mathf.Abs(map.m_width * map.m_scale - Size) > 0.01f)
                return false;
            Vector3 p = map.transform.position;
            return Mathf.Abs(p.x - Mathf.Round(p.x / Size) * Size) < 0.01f && Mathf.Abs(p.z - Mathf.Round(p.z / Size) * Size) < 0.01f;
        }

        private static long KeyOf(Heightmap map)
        {
            Vector3 p = map.transform.position;
            return Key(Mathf.RoundToInt(p.x / Size), Mathf.RoundToInt(p.z / Size));
        }

        private static long Key(int x, int z) => ((long)x << 32) | (uint)z;
    }
}
