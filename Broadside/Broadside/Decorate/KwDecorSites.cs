using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>A place a scene might stand: its middle's cell, its front's way, mirrored or not, and how much it likes it.</summary>
    public readonly struct KwSceneSite
    {
        public KwSceneSite(int cell, int dir, bool mirror, double score)
        {
            (Cell, Dir, Mirror, Score) = (cell, dir, mirror, score);
        }

        public int Cell { get; }
        public int Dir { get; }
        public bool Mirror { get; }
        public double Score { get; }
    }

    /// <summary>
    /// One scene's search for its place (DECORATE.md, "Scenes"), a row of the area at a time: every metre a middle is
    /// tried, its front turned by the scene's rule (<see cref="KwDecorFacing"/>), mirrored and not; a place goes on when the
    /// scene's needs are met (<see cref="KwDecorLikes.Meets"/>), its every core piece's footprint is on standable cells, its
    /// working space is walkable (or water, for a scene facing it) and nothing of it reaches into another scene's room
    /// (four lookups a rectangle, <see cref="KwDecorTable"/>). The best few by what it likes (a little at random, and never
    /// two within 2 m facing the same way) are then tried whole, one a step (<see cref="KwDecorStage"/>), the first that
    /// fits taken. A scene with nowhere it fits is left out.
    /// </summary>
    public sealed class KwSiteSearch : IKwDecorTask
    {
        private const int Keep = 16, Every = 2, Near = 4;
        private const double Chance = 1.5;
        private readonly KwDecorLayout layout;
        private readonly KwSceneShape shape;
        private readonly int copy;
        private readonly List<KwSceneSite> best = new List<KwSceneSite>();
        private int row = Every / 2, tried;

        public KwSiteSearch(KwDecorLayout layout, KwSceneShape shape, int copy)
        {
            (this.layout, this.shape, this.copy) = (layout, shape, copy);
        }

        public bool Step()
        {
            KwDecorGrid grid = layout.Area.Grid;
            if (row < grid.N)
            {
                for (int i = Every / 2; i < grid.N; i += Every)
                    Consider(grid, grid.Index(i, row));
                row += Every;
                return false;
            }
            return tried >= best.Count || KwDecorStage.Try(layout, shape, copy, best[tried++]);
        }

        private void Consider(KwDecorGrid grid, int cell)
        {
            if (!KwDecorFields.Walks(grid.Cells[cell]))
                return;
            int dir = KwDecorFacing.Of(layout, shape.Scene.Facing, cell);
            for (int m = 0; dir >= 0 && m < 2; m++)
            {
                KwSiteFrame frame = new KwSiteFrame(grid, cell, dir, m == 1);
                if (KwDecorLikes.Meets(layout, shape, copy, frame) && Room(grid, frame))
                    Offer(new KwSceneSite(cell, dir, m == 1, KwDecorLikes.Score(layout, shape, frame) + layout.Dice.NextDouble() * Chance), grid);
            }
        }

        // Every core piece on standable cells, the working spaces walkable, nothing in another scene's room.
        private bool Room(KwDecorGrid grid, KwSiteFrame frame)
        {
            KwDecorFields fields = layout.Area.Fields!;
            KwDecorTable stand = (shape.Scene.Needs & KwNeed.Land) != 0 ? fields.Land : fields.Ground;
            if (!Free(frame.Box(shape.Bounds.Grow(shape.Scene.Margin))))
                return false;
            foreach (KwScenePiece piece in shape.Pieces)
            {
                (int i0, int j0, int i1, int j1) = frame.Cells(grid, piece.Rect);
                if (piece.Stands && piece.Member.Extra == 0 && piece.Member.Place == KwDecorPlace.Ground && !stand.Clear(i0, j0, i1, j1, true))
                    return false;
            }
            foreach ((float x0, float z0, float x1, float z1) in shape.Scene.Aprons)
            {
                KwRect rect = new KwRect(x0, z0, x1, z1);
                (int i0, int j0, int i1, int j1) = frame.Cells(grid, rect);
                if (!(shape.Scene.ApronWet ? fields.WalkWet : fields.Walk).Clear(i0, j0, i1, j1, false) || !Free(frame.Box(rect)))
                    return false;
            }
            return true;
        }

        // Clear of every placed scene's room (plain loops: this runs for every place tried).
        private bool Free(KwDecorBox box)
        {
            foreach (KwPlacedScene scene in layout.Scenes)
            {
                foreach (KwDecorBox room in scene.Boxes)
                {
                    if (room.Overlaps(box, 0f))
                        return false;
                }
            }
            return true;
        }

        // Into the best few, sorted; a place within 2 m of a better one facing the same way is dropped.
        private void Offer(KwSceneSite site, KwDecorGrid grid)
        {
            for (int n = 0; n < best.Count; n++)
            {
                if (!Close(best[n], site, grid))
                    continue;
                if (best[n].Score >= site.Score)
                    return;
                best.RemoveAt(n);
                break;
            }
            int at = best.FindIndex(b => b.Score < site.Score);
            best.Insert(at < 0 ? best.Count : at, site);
            if (best.Count > Keep)
                best.RemoveAt(best.Count - 1);
        }

        private static bool Close(KwSceneSite a, KwSceneSite b, KwDecorGrid grid) =>
            a.Dir == b.Dir && Mathf.Abs(a.Cell % grid.N - b.Cell % grid.N) <= Near && Mathf.Abs(a.Cell / grid.N - b.Cell / grid.N) <= Near;
    }

    /// <summary>
    /// Which way a scene's front turns at a place (<see cref="KwFacing"/>): toward the water, the nearest path, the yard's
    /// heart, the water or the dry dock, or the most open walkable ground (a 6 m square 4.5 m out each way, the fewest cells
    /// nobody walks on, cells past the area's side counted as such; ties toward the heart). -1 when it has nowhere to face.
    /// </summary>
    public static class KwDecorFacing
    {
        private const float Out = 4.5f, Half = 3f, SlipWater = 8f;

        public static int Of(KwDecorLayout layout, KwFacing facing, int cell)
        {
            KwDecorArea area = layout.Area;
            switch (facing)
            {
                case KwFacing.Water: return area.WaterWay[cell];
                case KwFacing.Path: return area.Fields!.PathWay[cell] >= 0 ? area.Fields.PathWay[cell] : Open(area, cell);
                case KwFacing.Heart: return Toward(area.Grid, cell, area.Fields!.Heart);
                case KwFacing.Slip: return Slip(area, cell);
                default: return Open(area, cell);
            }
        }

        private static int Slip(KwDecorArea area, int cell)
        {
            if (area.WaterWay[cell] >= 0 && area.Grid.Cells[cell].Water <= SlipWater)
                return area.WaterWay[cell];
            return area.DryDock is KwDecorBox dock ? Toward(area.Grid, cell, dock.Centre) : -1;
        }

        private static int Toward(KwDecorGrid grid, int cell, Vector3 at)
        {
            Vector3 way = at - grid.On(cell);
            return way.x * way.x + way.z * way.z < 1f ? -1 : grid.DirOf(way);
        }

        private static int Open(KwDecorArea area, int cell)
        {
            KwDecorGrid grid = area.Grid;
            Vector3 at = grid.On(cell), heart = area.Fields!.Heart - at;
            (int best, float least) = (-1, float.MaxValue);
            for (int dir = 0; dir < 4; dir++)
            {
                float bad = Blocked(area, at + grid.Dir(dir) * Out) - 0.01f * Vector3.Dot(grid.Dir(dir), heart.normalized);
                if (bad < least)
                    (best, least) = (dir, bad);
            }
            return best;
        }

        // Cells nobody walks on in a square about a point, those past the area's side counted.
        private static int Blocked(KwDecorArea area, Vector3 middle)
        {
            KwDecorGrid grid = area.Grid;
            Vector2 c = grid.Coords(middle);
            int r = Mathf.RoundToInt(Half / KwDecorGrid.Step), i0 = Mathf.RoundToInt(c.x) - r, j0 = Mathf.RoundToInt(c.y) - r;
            int i1 = i0 + 2 * r, j1 = j0 + 2 * r;
            int wide = Mathf.Max(0, Mathf.Min(grid.N, i1) - Mathf.Max(0, i0)), deep = Mathf.Max(0, Mathf.Min(grid.N, j1) - Mathf.Max(0, j0));
            return area.Fields!.Walk.Count(i0, j0, i1, j1) + 4 * r * r - wide * deep;
        }
    }
}
