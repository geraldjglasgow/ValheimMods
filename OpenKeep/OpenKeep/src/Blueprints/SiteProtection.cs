using System;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Whether this player may build and shape the ground on a site, checked like the game checks a hand placed piece,
    /// at points every few metres over the whole work area (where pieces stand and where the ground moves): not inside
    /// a dungeon or a building interior, not where the game forbids building, not under a ward the player has no
    /// access to.
    /// </summary>
    public static class SiteProtection
    {
        private const float Spacing = 4f;

        /// <summary>A blueprint's site: its pieces' rectangle and its ground work.</summary>
        public static string Reason(SitePlan plan)
        {
            Rect area = SiteArea.World(plan.Blueprint, plan.Frame, Grow(plan.Work));
            return Reason(plan.Frame.Origin, area, plan.Work, point => InPieces(point, plan));
        }

        /// <summary>The reason a site is protected, or null. <paramref name="building"/> says where pieces stand.</summary>
        public static string Reason(Vector3 origin, Rect area, GroundWork work, Func<Vector3, bool> building)
        {
            if (Character.InInterior(origin))
                return BlueprintWords.Indoors;
            for (float x = area.xMin; x <= area.xMax + 0.01f; x += Spacing)
            {
                for (float z = area.yMin; z <= area.yMax + 0.01f; z += Spacing)
                {
                    Vector3 point = new Vector3(x, origin.y, z);
                    if (!building(point) && work.MoveAt(point) <= 0f)
                        continue;
                    string reason = At(point);
                    if (reason != null)
                        return reason;
                }
            }
            return null;
        }

        private static string At(Vector3 point)
        {
            if (Location.IsInsideNoBuildLocation(point))
                return BlueprintWords.NoBuild;
            return PrivateArea.CheckAccess(point, Spacing, flash: false, wardCheck: false) ? null : BlueprintWords.Warded;
        }

        /// <summary>Inside the blueprint's pieces' rectangle, plus the clearing margin and a little for the pieces' own size.</summary>
        public static bool InPieces(Vector3 point, SitePlan plan)
        {
            Vector2 local = plan.Frame.Local(point.x, point.z);
            Rect r = plan.Blueprint.PieceBounds;
            float m = BlueprintRules.ClearMargin + 2f;
            return local.x >= r.xMin - m && local.x <= r.xMax + m && local.y >= r.yMin - m && local.y <= r.yMax + m;
        }

        /// <summary>How far beyond the target's own area the ground work can reach.</summary>
        public static float Grow(GroundWork work)
        {
            return BlueprintRules.ClearMargin + Mathf.Min(BlueprintRules.SkirtReach,
                Mathf.Max(work.MaxCut / BlueprintRules.CutSlope, work.MaxFill / BlueprintRules.FillSlope));
        }
    }
}
