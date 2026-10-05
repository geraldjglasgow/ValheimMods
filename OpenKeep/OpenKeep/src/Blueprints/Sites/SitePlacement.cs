using OpenKeep.Core;
using Splatform;
using UnityEngine;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// Places a construction site for a checked blueprint (the second click with a pinned blueprint): the post, made
    /// on the placing machine (which owns its new ZDO and writes the site's state into it at once), standing 2 m in
    /// front of the blueprint's front edge at the height its ground will have once shaped, with the creator, the
    /// blueprint, the frame and the ground's net stone. Refused where another loaded site's pieces already stand
    /// (their pivots' rectangles overlap). Nothing is built or charged here; <see cref="SiteBuilder"/> does that as
    /// materials are handed over.
    /// </summary>
    public static class SitePlacement
    {
        /// <summary>How far in front of the blueprint's front-most pieces the post stands, metres.</summary>
        private const float PostDistance = 2f;

        public static bool Place(Player player, SitePlan plan)
        {
            string refusal = Refusal(plan);
            if (refusal != null)
            {
                Messages.Center(refusal);
                return false;
            }
            GameObject go = Object.Instantiate(SitePrefab.Prefab, PostPoint(plan), plan.Frame.Rotation);
            SiteMarker site = go.GetComponent<SiteMarker>();
            if (site == null || site.State == null)
            {
                ZNetScene.instance.Destroy(go);
                return false;
            }
            site.State.Init(plan.Blueprint, plan.Frame, player);
            site.State.SetGroundStone(plan.Work.StoneNeeded - plan.Work.StoneRemoved);
            go.GetComponent<Piece>()?.SetCreator(player.GetPlayerID(), PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            BuildUndo.Begin(site, plan.Blueprint, plan.Frame);
            Messages.Center(BlueprintWords.Format(SiteWords.Placed, plan.Blueprint.Name));
            Plugin.Log.LogInfo($"OpenKeep: construction site {plan.Blueprint.Name} placed at {plan.Frame.Origin} yaw {plan.Frame.Yaw:0} " +
                $"({plan.Blueprint.Pieces.Count} pieces, ground stone {plan.Work.StoneNeeded - plan.Work.StoneRemoved})");
            return true;
        }

        /// <summary>Why no site can go here (beyond the plan's own checks), or null.</summary>
        private static string Refusal(SitePlan plan)
        {
            if (SitePrefab.Prefab == null)
                return Language.Localize(SiteWords.NoPrefab);
            Rect mine = PieceRect(plan.Blueprint, plan.Frame);
            foreach (SiteMarker other in SiteMarker.Loaded)
            {
                Blueprint bp = other != null ? other.State?.Blueprint : null;
                if (bp != null && PieceRect(bp, other.State.Frame).Overlaps(mine))
                    return BlueprintWords.Format(SiteWords.Overlaps, other.State.Name);
            }
            return null;
        }

        /// <summary>The world rectangle (x, z) holding a blueprint's piece pivots at a frame.</summary>
        private static Rect PieceRect(Blueprint bp, BuildFrame frame)
        {
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (Vector2 c in SiteArea.Corners(bp.PieceBounds, 0f))
            {
                Vector3 w = frame.World(c.x, 0f, c.y);
                minX = Mathf.Min(minX, w.x);
                maxX = Mathf.Max(maxX, w.x);
                minZ = Mathf.Min(minZ, w.z);
                maxZ = Mathf.Max(maxZ, w.z);
            }
            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        /// <summary>In front of the middle of the front edge, on the ground as the ground work will leave it.</summary>
        private static Vector3 PostPoint(SitePlan plan)
        {
            Rect r = plan.Blueprint.PieceBounds;
            Vector3 at = plan.Frame.World(r.center.x, 0f, r.yMin - PostDistance);
            at.y = GroundAfterWork(plan.Work, at) ?? (Heightmap.GetHeight(at, out float height) ? height : plan.Frame.Ground);
            return at;
        }

        private static float? GroundAfterWork(GroundWork work, Vector3 at)
        {
            int x = Mathf.RoundToInt(at.x), z = Mathf.RoundToInt(at.z);
            foreach (GroundPoint p in work.Points)
            {
                if (p.X == x && p.Z == z && p.Moves)
                    return p.Height;
            }
            return null;
        }
    }
}
