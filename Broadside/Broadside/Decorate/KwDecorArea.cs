using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>A building wall's face beside a walkable cell (<see cref="KwDecorWalls"/>): where a wall piece hangs.</summary>
    public readonly struct KwWallSpot
    {
        public KwWallSpot(int cell, Vector3 point, Vector3 normal)
        {
            (Cell, Point, Normal) = (cell, point, normal);
        }

        public int Cell { get; }

        /// <summary>Where the ray met the face, 1.2 m over the cell's surface.</summary>
        public Vector3 Point { get; }

        /// <summary>The face's normal across the ground, out of the wall.</summary>
        public Vector3 Normal { get; }
    }

    /// <summary>
    /// What one reading found (DECORATE.md, "Reading the area"), plain data the layout works from: the cells, the wall
    /// faces, where docks are stepped onto, the ends of the ways between doors, ladders and stations, the yards (the dry
    /// dock, ships, workbenches) and the dry dock's box, the zones kept clear, the decorations already standing, each
    /// cell's way to the water, and the fields worked out last (<see cref="KwDecorFields"/>). Filled by
    /// <see cref="KwDecorRead"/> over frames; nothing here is sent or saved.
    /// </summary>
    public sealed class KwDecorArea
    {
        public KwDecorArea(KwDecorGrid grid)
        {
            Grid = grid;
            WaterWay = new sbyte[grid.Cells.Length];
            for (int k = 0; k < WaterWay.Length; k++)
                WaterWay[k] = -1;
        }

        public KwDecorGrid Grid { get; }
        public List<KwWallSpot> Walls { get; } = new List<KwWallSpot>();

        /// <summary>Where each dock is stepped onto (path ends, the quay's root).</summary>
        public List<Vector3> Entries { get; } = new List<Vector3>();

        /// <summary>Doors' sides, ladders' and stairs' feet, stations' fronts, portals' sides (path ends).</summary>
        public List<Vector3> Ways { get; } = new List<Vector3>();

        /// <summary>The dry dock, ships and workbenches near the area.</summary>
        public List<Vector3> Yards { get; } = new List<Vector3>();

        /// <summary>The dry dock with the ship designed on it, grown by its margins, when one is near (its keel line runs along the longer side).</summary>
        public KwDecorBox? DryDock { get; set; }

        /// <summary>The zones kept clear, marked into the cells (<see cref="KwDecorClear"/>).</summary>
        public List<KwDecorBox> Kept { get; } = new List<KwDecorBox>();

        /// <summary>Decorations already standing in and round the area: their footprints and spacing, kept from.</summary>
        public List<(KwDecorBox Box, float Spacing)> Existing { get; } = new List<(KwDecorBox, float)>();

        /// <summary>Each cell's way (a grid direction) to the nearest water within 16 m; -1 for none.</summary>
        public sbyte[] WaterWay { get; }

        /// <summary>The fields worked out once the cells are read (null until then).</summary>
        public KwDecorFields? Fields { get; set; }
    }
}
