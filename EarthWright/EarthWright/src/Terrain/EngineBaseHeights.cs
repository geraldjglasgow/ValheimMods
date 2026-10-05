using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// The heights of each heightmap before its compiler's edits: the generated ground plus the flattening the game's
    /// locations (villages, dungeon entrances) apply through their terrain modifiers. The compiler's deltas are added to
    /// exactly these heights, so levelling to a target, the height limits and "reset" all measure from them (the plain
    /// generated heights would be off by the location's flattening). Captured by the patched ApplyToHeightmap every time
    /// a heightmap with a compiler rebuilds, on every machine; held weakly, so unloaded heightmaps are forgotten.
    /// </summary>
    public static class EngineBaseHeights
    {
        private static readonly ConditionalWeakTable<Heightmap, float[]> cache = new ConditionalWeakTable<Heightmap, float[]>();

        /// <summary>Every client, heightmap rebuild: remembers the heights before the compiler is applied.</summary>
        internal static void Capture(Heightmap map, List<float> heights)
        {
            if (!cache.TryGetValue(map, out float[] stored) || stored.Length != heights.Count)
            {
                if (stored != null)
                    cache.Remove(map);
                stored = new float[heights.Count];
                cache.Add(map, stored);
            }
            heights.CopyTo(stored);
        }

        /// <summary>
        /// The pre-edit heights of a heightmap with a compiler: the captured ones, or before its first rebuild with the
        /// compiler (a compiler created a moment ago, with no edits yet) the shown heights minus the compiler's deltas.
        /// </summary>
        internal static float[] For(Heightmap map, TerrainComp comp, ref float[] scratch)
        {
            if (Captured(map, out float[] stored))
                return stored;
            int count = map.m_heights.Count;
            Ensure(ref scratch, count);
            for (int i = 0; i < count; i++)
                scratch[i] = map.m_heights[i] - comp.m_levelDelta[i] - comp.m_smoothDelta[i];
            return scratch;
        }

        /// <summary>The heights of a heightmap nobody has edited (they are its pre-edit heights).</summary>
        internal static float[] CopyShown(Heightmap map, ref float[] scratch)
        {
            Ensure(ref scratch, map.m_heights.Count);
            map.m_heights.CopyTo(scratch);
            return scratch;
        }

        /// <summary>
        /// The world height of a heightmap vertex before any terrain edit (the ground the limits measure from), false when
        /// the index is not valid. For other modules that check heights against <see cref="HeightLimits"/>.
        /// </summary>
        public static bool TryWorld(Heightmap map, int index, out float height)
        {
            height = 0f;
            if (!Valid(map, index))
                return false;
            height = (Captured(map, out float[] stored) ? stored[index] : Uncaptured(map, TerrainRead.CompilerOf(map), index)) + map.transform.position.y;
            return true;
        }

        /// <summary>
        /// The same height relative to the heightmap (add its position's y), with its compiler already found (null when it
        /// has none), for readers of many vertices that keep the compiler and position at hand.
        /// </summary>
        internal static bool TryLocal(Heightmap map, TerrainComp comp, int index, out float local)
        {
            local = 0f;
            if (!Valid(map, index))
                return false;
            local = Captured(map, out float[] stored) ? stored[index] : Uncaptured(map, comp, index);
            return true;
        }

        private static bool Valid(Heightmap map, int index) => map != null && index >= 0 && index < map.m_heights.Count;

        private static bool Captured(Heightmap map, out float[] stored) => cache.TryGetValue(map, out stored) && stored.Length == map.m_heights.Count;

        /// <summary>A heightmap not rebuilt with a compiler yet: its shown height, less any deltas a compiler already holds.</summary>
        private static float Uncaptured(Heightmap map, TerrainComp comp, int index)
        {
            float local = map.m_heights[index];
            if (comp != null && comp.m_initialized && index < comp.m_levelDelta.Length)
                local -= comp.m_levelDelta[index] + comp.m_smoothDelta[index];
            return local;
        }

        /// <summary>The pre-edit world height at the vertex nearest to a world position, or the fallback.</summary>
        public static float At(Vector3 world, float fallback)
        {
            Heightmap map = Heightmap.FindHeightmap(world);
            if (map == null)
                return fallback;
            map.WorldToVertex(world, out int x, out int y);
            int pitch = map.m_width + 1;
            if (x < 0 || y < 0 || x >= pitch || y >= pitch)
                return fallback;
            return TryWorld(map, y * pitch + x, out float height) ? height : fallback;
        }

        private static void Ensure(ref float[] scratch, int count)
        {
            if (scratch == null || scratch.Length != count)
                scratch = new float[count];
        }
    }
}
