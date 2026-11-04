using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A scene tried whole at a place (DECORATE.md, "Scenes"): each piece where the scene's frame puts it, on the surface
    /// there, turned with the scene a degree or so off at random, and every check a decoration has
    /// (<see cref="KwDecorFitCheck"/>: its cells, apart from other scenes' pieces and those standing, the game's rules,
    /// nothing solid in its way); a wall piece on the nearest building wall within 7 m that faces the scene. Every core
    /// piece must fit and the core must stand within 1.2 m of height, or nothing of the scene is placed; a filler that
    /// does not fit is left out. Placed, its pieces join the layout with the density each first shows at (the scene's, a
    /// second copy's later, a filler's later still) and the scene's room is kept from the scenes after it.
    /// </summary>
    public static class KwDecorStage
    {
        private const float Uneven = 1.2f, WallReach = 7f;
        private const int WallTries = 12;

        public static bool Try(KwDecorLayout layout, KwSceneShape shape, int copy, KwSceneSite site)
        {
            KwDecorScene scene = shape.Scene;
            KwSiteFrame frame = new KwSiteFrame(layout.Area.Grid, site.Cell, site.Dir, site.Mirror);
            int group = layout.Group(scene.Name), level = scene.Level + copy * scene.Later;
            List<KwDecorPlan> plans = new List<KwDecorPlan>();
            foreach (KwScenePiece piece in shape.Pieces)
            {
                int at = Mathf.Min(KwDecorLayout.Densities, level + piece.Member.Extra);
                KwDecorPlan? plan = piece.Stands ? Standing(layout, frame, piece, at, group) : OnWall(layout, frame, piece, at, group);
                if (plan != null)
                    plans.Add(plan);
                else if (piece.Member.Extra == 0)
                    return false;
            }
            if (!Even(plans))
                return false;
            layout.Plans.AddRange(plans);
            layout.Scenes.Add(new KwPlacedScene(scene.Id, frame.Origin, Room(shape, frame)));
            return true;
        }

        private static KwDecorPlan? Standing(KwDecorLayout layout, KwSiteFrame frame, KwScenePiece piece, int level, int group)
        {
            KwDecorGrid grid = layout.Area.Grid;
            Vector3 at = frame.World(piece.Member.X, piece.Member.Z);
            if (!grid.CellOf(at, out int k))
                return null;
            at.y = grid.Cells[k].Y;
            KwSlot slot = new KwSlot(piece.Member.Place, k, at, Vector3.zero, false, grid.Cells[k].Region);
            KwDecorPlan plan = new KwDecorPlan(piece.Kind, at, KwDecorPose.Free(frame.YawOf(piece.Member), layout.Dice), slot, level, group);
            return KwDecorFitCheck.Fits(layout.Area, plan, layout.Plans) ? plan : null;
        }

        // The nearest wall faces within reach that face the scene, the first that takes the piece.
        private static KwDecorPlan? OnWall(KwDecorLayout layout, KwSiteFrame frame, KwScenePiece piece, int level, int group)
        {
            KwDecorArea area = layout.Area;
            List<KwWallSpot> spots = area.Walls.FindAll(w => KwDecorLikes.Flat(w.Point - frame.Origin) <= WallReach && Vector3.Dot(w.Normal, frame.Origin - w.Point) > 0f);
            spots.Sort((a, b) => KwDecorLikes.Flat(a.Point - frame.Origin).CompareTo(KwDecorLikes.Flat(b.Point - frame.Origin)));
            for (int n = 0; n < Mathf.Min(WallTries, spots.Count); n++)
            {
                KwCell cell = area.Grid.Cells[spots[n].Cell];
                Vector3 at = new Vector3(spots[n].Point.x, cell.Y, spots[n].Point.z);
                KwSlot slot = new KwSlot(KwDecorPlace.Wall, spots[n].Cell, at, -spots[n].Normal, true, cell.Region);
                (Vector3 position, Quaternion turn) = KwDecorPose.Hung(slot, piece.Kind.Fit);
                KwDecorPlan plan = new KwDecorPlan(piece.Kind, position, turn, slot, level, group);
                if (KwDecorFitCheck.Fits(area, plan, layout.Plans))
                    return plan;
            }
            return null;
        }

        // The standing pieces within 1.2 m of height (a scene never straddles a terrace).
        private static bool Even(List<KwDecorPlan> plans)
        {
            (float low, float high) = (float.MaxValue, float.MinValue);
            foreach (KwDecorPlan plan in plans)
            {
                if (plan.Slot.Place != KwDecorPlace.Wall)
                    (low, high) = (Mathf.Min(low, plan.Position.y), Mathf.Max(high, plan.Position.y));
            }
            return high - low <= Uneven;
        }

        // The scene's footprint with its margin, and its working spaces.
        private static List<KwDecorBox> Room(KwSceneShape shape, KwSiteFrame frame)
        {
            List<KwDecorBox> boxes = new List<KwDecorBox> { frame.Box(shape.Bounds.Grow(shape.Scene.Margin)) };
            foreach ((float x0, float z0, float x1, float z1) in shape.Scene.Aprons)
                boxes.Add(frame.Box(new KwRect(x0, z0, x1, z1)));
            return boxes;
        }
    }
}
