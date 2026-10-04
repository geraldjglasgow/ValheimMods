using UnityEngine;

namespace DevBridge.Overlay
{
    /// <summary>
    /// Edited ground as each zone's terrain compiler (TerrainComp) holds it: not a list of edits but a flag per heightmap
    /// vertex (m_modifiedHeight: levelled, raised, dug or smoothed) and per paint cell (m_modifiedPaint: cultivated,
    /// paved, cleared). Drawn as a hatch on the ground, one line along each run of flagged points in a row, 1 m apart:
    /// orange for edited height, faint yellow for paint (its cells sit half a cell off the height vertices).
    /// </summary>
    internal static class TerrainEdits
    {
        private static readonly Color Height = new Color(1f, 0.55f, 0.15f, 0.85f);
        private static readonly Color Paint = new Color(1f, 0.9f, 0.3f, 0.5f);

        internal static void Draw(OverlayArea area, Category into)
        {
            foreach (TerrainComp comp in TerrainComp.s_instances)
            {
                if (!comp || !comp.m_initialized || !comp.m_hmap) continue;
                if (Utils.DistanceXZ(comp.transform.position, area.Centre) > area.Radius + comp.m_size * 0.71f) continue;
                into.Count("compilers");
                into.Count("height_points", Hatch(comp, comp.m_modifiedHeight, 0f, Height, area, into));
                into.Count("paint_points", Hatch(comp, comp.m_modifiedPaint, 0.5f, Paint, area, into));
            }
        }

        // Each row's runs of flagged points inside the area, one line per run; returns how many points were flagged.
        private static int Hatch(TerrainComp comp, bool[] flagged, float shift, Color colour, OverlayArea area, Category into)
        {
            int pitch = comp.m_width + 1, count = 0;
            if (flagged == null || flagged.Length < pitch * pitch) return 0;
            for (int row = 0; row < pitch; row++)
            {
                int start = -1;
                for (int column = 0; column <= pitch; column++)
                {
                    bool on = column < pitch && flagged[row * pitch + column] && area.Holds(Vertex(comp.m_hmap, column, row, shift));
                    if (on) count++;
                    if (on && start < 0) start = column;
                    if (on || start < 0) continue;
                    into.Lines.Add(Run(comp.m_hmap, row, start, column - 1, shift), colour, 0.04f);
                    start = -1;
                }
            }
            return count;
        }

        // A run of points along a row, stretched 0.4 of a cell past each end so a single point still shows as a dash.
        private static Vector3[] Run(Heightmap map, int row, int from, int to, float shift)
        {
            var points = new Vector3[to - from + 3];
            for (int column = from; column <= to; column++) points[column - from + 1] = Vertex(map, column, row, shift) + Vector3.up * 0.1f;
            Vector3 step = Vector3.right * map.m_scale * 0.4f;
            points[0] = points[1] - step;
            points[points.Length - 1] = points[points.Length - 2] + step;
            return points;
        }

        // Where Heightmap.CalcVertex puts a vertex: the heightmap's centre, offset by whole cells, at its stored height.
        private static Vector3 Vertex(Heightmap map, int column, int row, float shift)
        {
            int half = map.m_width / 2;
            var local = new Vector3((column - half - shift) * map.m_scale, map.GetHeight(column, row), (row - half - shift) * map.m_scale);
            return map.transform.position + local;
        }
    }
}
