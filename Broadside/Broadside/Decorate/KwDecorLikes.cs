using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a scene needs and likes at a place (<see cref="KwNeed"/>, <see cref="KwLike"/>; DECORATE.md, "Scenes"), read
    /// at three points of it: its middle, a point half a metre in front of its core and one half a metre behind. Needs are
    /// yes or no; likes add up to a score (a near yard 3, a wall at its back 1.5, the water's edge -3 for a dry one, ...),
    /// a little chance added by the caller. A second copy of a scene stands at least its <see cref="KwDecorScene.Apart"/>
    /// from the first, and a scene of one piece (a vane, a heap, an anchor) never within 16 m of the same piece placed by
    /// another scene. Plain arithmetic on the reading's fields.
    /// </summary>
    public static class KwDecorLikes
    {
        private const float FrontWater = 1.5f, SlipWater = 3f, SlipDock = 6f, NearWater = 14f, QuayReach = 7f;
        private const float YardReach = 12f, Beside = 14f, Twin = 16f;
        private const int PathFront = 5, BackWall = 6, NearPath = 8;

        public static bool Meets(KwDecorLayout layout, KwSceneShape shape, int copy, KwSiteFrame frame)
        {
            KwNeed needs = shape.Scene.Needs;
            KwDecorArea area = layout.Area;
            Vector3 front = frame.World(0f, shape.Front + 0.5f);
            if ((needs & KwNeed.WaterFront) != 0 && Water(area, front) > FrontWater)
                return false;
            if ((needs & KwNeed.Slip) != 0 && Water(area, front) > SlipWater && !(area.DryDock is KwDecorBox dock && dock.Distance(front) <= SlipDock))
                return false;
            if ((needs & KwNeed.NearWater) != 0 && Water(area, frame.Origin) > NearWater)
                return false;
            if ((needs & KwNeed.Quay) != 0 && Nearest(area.Entries, frame.Origin) > QuayReach)
                return false;
            if ((needs & KwNeed.PathFront) != 0 && !Within(area.Fields!.PathSteps, area.Grid, front, PathFront))
                return false;
            if (shape.Pieces.Count == 1 && Twinned(layout, shape.Pieces[0].Kind, frame.Origin))
                return false;
            return copy == 0 || NearestScene(layout, shape.Scene.Id, frame.Origin) >= shape.Scene.Apart;
        }

        // The same piece within 16 m, placed by another scene (plain loops: this runs for every place tried).
        private static bool Twinned(KwDecorLayout layout, KwDecorKind kind, Vector3 at)
        {
            foreach (KwDecorPlan plan in layout.Plans)
            {
                if (plan.Kind == kind && Flat(plan.Position - at) < Twin)
                    return true;
            }
            return false;
        }

        private static float NearestScene(KwDecorLayout layout, string id, Vector3 at)
        {
            float best = float.MaxValue;
            foreach (KwPlacedScene scene in layout.Scenes)
            {
                if (scene.Id == id)
                    best = Mathf.Min(best, Flat(scene.At - at));
            }
            return best;
        }

        public static double Score(KwDecorLayout layout, KwSceneShape shape, KwSiteFrame frame)
        {
            KwLike likes = shape.Scene.Likes;
            KwDecorArea area = layout.Area;
            Vector3 at = frame.Origin, back = frame.World(0f, shape.Back - 0.5f);
            double score = Has(likes, KwLike.Yard) && NearYard(area, at) ? 3.0 : 0.0;
            score += Has(likes, KwLike.BackToWall) && Within(area.Fields!.WallSteps, area.Grid, back, BackWall) ? 1.5 : 0.0;
            score += Has(likes, KwLike.Dry) ? Dry(Water(area, at)) : 0.0;
            score += Has(likes, KwLike.Central) ? -0.06 * Flat(at - area.Fields!.Heart) : 0.0;
            score += Has(likes, KwLike.Outskirts) ? Outskirts(area, at) : 0.0;
            score += Has(likes, KwLike.High) ? 2.0 * (at.y - area.Fields!.Low) / Mathf.Max(1f, area.Fields.High - area.Fields.Low) : 0.0;
            score += Has(likes, KwLike.Entrance) ? Close(Nearest(area.Fields!.Entrances, at), 12f, 3.0) : 0.0;
            score += Has(likes, KwLike.Path) && Within(area.Fields!.PathSteps, area.Grid, at, NearPath) ? 1.5 : 0.0;
            score += Has(likes, KwLike.Quay) ? Close(Nearest(area.Entries, at), 15f, 3.0) : 0.0;
            score += Has(likes, KwLike.Wet) && Water(area, at) < 8f ? 1.0 : 0.0;
            return score + BesideScore(layout, shape.Scene.Beside, at);
        }

        private static bool Has(KwLike likes, KwLike like) => (likes & like) != 0;

        // Back from the water's edge: -3 within 4 m, -1 within 7 m.
        private static double Dry(float water) => water < 4f ? -3.0 : water < 7f ? -1.0 : 0.0;

        // Far from the heart, and 2 more within 5 m of the area's side.
        private static double Outskirts(KwDecorArea area, Vector3 at)
        {
            Vector2 c = area.Grid.Coords(at);
            float side = Mathf.Min(Mathf.Min(c.x, c.y), Mathf.Min(area.Grid.N - c.x, area.Grid.N - c.y)) * KwDecorGrid.Step;
            return 0.08 * Flat(at - area.Fields!.Heart) + (side <= 5f ? 2.0 : 0.0);
        }

        // The most within a reach, falling to nothing at it.
        private static double Close(float distance, float reach, double most) => distance < reach ? most * (1.0 - distance / reach) : 0.0;

        private static double BesideScore(KwDecorLayout layout, string? id, Vector3 at) => id == null ? 0.0 : Close(NearestScene(layout, id, at), Beside, 2.5);

        /// <summary>Within a yard's reach: the dry dock within 12 m, or a ship or workbench within 14.</summary>
        public static bool NearYard(KwDecorArea area, Vector3 at) =>
            (area.DryDock is KwDecorBox dock && dock.Distance(at) <= YardReach) || Nearest(area.Yards, at) <= YardReach + 2f;

        /// <summary>Metres to the nearest water from a point in the area (16, the reading's reach, outside it).</summary>
        public static float Water(KwDecorArea area, Vector3 at) => area.Grid.CellOf(at, out int k) ? area.Grid.Cells[k].Water : 16f;

        private static bool Within(int[] steps, KwDecorGrid grid, Vector3 at, int most) => grid.CellOf(at, out int k) && steps[k] >= 0 && steps[k] <= most;

        /// <summary>Level metres to the nearest of some points; far with none.</summary>
        public static float Nearest(List<Vector3> points, Vector3 at)
        {
            float best = float.MaxValue;
            foreach (Vector3 point in points)
                best = Mathf.Min(best, Flat(point - at));
            return best;
        }

        public static float Flat(Vector3 d) => Mathf.Sqrt(d.x * d.x + d.z * d.z);
    }
}
