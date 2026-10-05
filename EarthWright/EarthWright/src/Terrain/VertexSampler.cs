using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Terrain
{
    /// <summary>
    /// <see cref="TerrainRead.TryVertex"/> for loops over many vertices, with the same answers: each heightmap, its
    /// position and its compiler are found once per pass instead of once per vertex (the game's searches walk every loaded
    /// heightmap and compiler each time). A pass is one synchronous loop opened with <see cref="Begin"/>: the game creates
    /// and destroys nothing while it runs, so what is remembered is exactly what the searches would answer, and the next
    /// pass (or the next frame, should a caller forget) starts empty. Zone heightmaps tile the world on whole metres, so a
    /// point strictly inside a remembered heightmap lies in no other; a point on an edge (a vertex two neighbours both
    /// hold) goes to the game's search, which picks between them as it always did.
    /// </summary>
    public sealed class VertexSampler
    {
        private struct Area
        {
            public Heightmap Map;
            public TerrainComp Comp;
            public Vector3 Origin;
            public float MinX, MaxX, MinZ, MaxZ;
        }

        private readonly List<Area> areas = new List<Area>();
        private int frame = -1;

        /// <summary>Opens a pass: forgets the heightmaps and compilers of the previous one.</summary>
        public void Begin()
        {
            areas.Clear();
            frame = Time.frameCount;
        }

        /// <summary>The vertex nearest to the position, false when no loaded heightmap covers it.</summary>
        public bool TryVertex(Vector3 world, out VertexInfo info)
        {
            if (frame != Time.frameCount)
                Begin();
            int i = Find(world);
            if (i < 0)
            {
                info = default;
                return false;
            }
            Area area = areas[i];
            return TerrainRead.TryRead(area.Map, area.Comp, area.Origin, world, out info);
        }

        /// <summary>The world's generated height at a world position (nearest vertex), or the fallback.</summary>
        public float BaseHeight(Vector3 world, float fallback)
        {
            return TryVertex(world, out VertexInfo info) ? info.Base : fallback;
        }

        /// <summary>The remembered heightmap holding the point, the game's choice when none holds it strictly; -1 when none is loaded there.</summary>
        private int Find(Vector3 world)
        {
            for (int i = 0; i < areas.Count; i++)
            {
                Area area = areas[i];
                if (world.x > area.MinX && world.x < area.MaxX && world.z > area.MinZ && world.z < area.MaxZ && area.Map != null)
                    return i;
            }
            Heightmap map = Heightmap.FindHeightmap(world);
            return map != null ? Remember(map) : -1;
        }

        private int Remember(Heightmap map)
        {
            for (int i = 0; i < areas.Count; i++)
            {
                if (areas[i].Map == map)
                    return i;
            }
            areas.Add(Measure(map));
            return areas.Count - 1;
        }

        /// <summary>The heightmap's square exactly as <c>Heightmap.IsPointInside</c> computes it, and its compiler.</summary>
        private static Area Measure(Heightmap map)
        {
            Vector3 origin = map.transform.position;
            float half = (float)((double)map.m_width * (double)map.m_scale * 0.5);
            return new Area
            {
                Map = map,
                Comp = TerrainRead.CompilerOf(map),
                Origin = origin,
                MinX = (float)((double)origin.x - (double)half),
                MaxX = (float)((double)origin.x + (double)half),
                MinZ = (float)((double)origin.z - (double)half),
                MaxZ = (float)((double)origin.z + (double)half),
            };
        }
    }
}
