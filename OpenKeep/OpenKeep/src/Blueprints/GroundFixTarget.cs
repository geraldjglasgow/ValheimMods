using System.Collections.Generic;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// A building as a ground target for Fix ground. The pieces that touch the ground (bottom at most
    /// <see cref="BlueprintRules.TouchHeight"/> above it, or below it) mark the ground they cover: under a floor (a piece
    /// at most <see cref="BlueprintRules.FloorHeight"/> tall) the ground is cut or filled to just under its bottom; under
    /// anything taller (walls, posts, stairs) it is only filled up to the bottom, never dug out from around a buried
    /// foundation. That ground is painted dirt. Everything else is left to the slopes.
    /// </summary>
    internal sealed class GroundFixTarget : IGroundTarget
    {
        private sealed class Cell
        {
            public float Floor = float.NaN;
            public float Other = float.NaN;
        }

        private readonly Dictionary<long, Cell> cells = new Dictionary<long, Cell>();

        /// <summary>The pieces that touch the ground.</summary>
        public readonly List<FixPiece> Touching = new List<FixPiece>();

        /// <summary>The world rectangle (x, z) holding the touching pieces' footprints.</summary>
        public Rect Area { get; private set; }

        public static GroundFixTarget From(List<FixPiece> building)
        {
            GroundFixTarget target = new GroundFixTarget();
            foreach (FixPiece p in building)
            {
                if (Heightmap.GetHeight(p.Centre, out float ground) && p.Bottom <= ground + BlueprintRules.TouchHeight)
                    target.Mark(p);
            }
            return target;
        }

        /// <summary>A vertex under a touching piece: its pad height and dirt; NaN elsewhere.</summary>
        public float Pad(int x, int z, float current, out GroundPaint paint)
        {
            paint = GroundPaint.None;
            if (!cells.TryGetValue(GroundWork.Key(x, z), out Cell cell))
                return float.NaN;
            paint = GroundPaint.Dirt;
            if (!float.IsNaN(cell.Floor))
                return cell.Floor - BlueprintRules.UnderGap;
            return float.IsNaN(current) ? float.NaN : Mathf.Max(current, cell.Other - BlueprintRules.UnderGap);
        }

        /// <summary>The vertices a piece covers take its bottom: the lowest floor, or the lowest of the rest.</summary>
        private void Mark(FixPiece p)
        {
            Touching.Add(p);
            Rect box = p.Box;
            Area = Touching.Count == 1 ? box : Rect.MinMaxRect(Mathf.Min(Area.xMin, box.xMin), Mathf.Min(Area.yMin, box.yMin),
                Mathf.Max(Area.xMax, box.xMax), Mathf.Max(Area.yMax, box.yMax));
            bool floor = p.Height <= BlueprintRules.FloorHeight;
            for (int x = Mathf.FloorToInt(box.xMin); x <= Mathf.CeilToInt(box.xMax); x++)
            {
                for (int z = Mathf.FloorToInt(box.yMin); z <= Mathf.CeilToInt(box.yMax); z++)
                {
                    if (p.Covers(x, z))
                        Put(GroundWork.Key(x, z), floor, p.Bottom);
                }
            }
        }

        private void Put(long key, bool floor, float bottom)
        {
            if (!cells.TryGetValue(key, out Cell cell))
                cells[key] = cell = new Cell();
            if (floor)
                cell.Floor = float.IsNaN(cell.Floor) ? bottom : Mathf.Min(cell.Floor, bottom);
            else
                cell.Other = float.IsNaN(cell.Other) ? bottom : Mathf.Min(cell.Other, bottom);
        }

        /// <summary>A world vertex lies under a touching piece.</summary>
        public bool Under(Vector3 point) => cells.ContainsKey(GroundWork.Key(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.z)));
    }
}
