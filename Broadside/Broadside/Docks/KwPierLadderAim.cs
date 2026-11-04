using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What the ladder tool's aim means on a dock (<see cref="KwPierLadderTool"/>), on the aiming player's game, along the
    /// game's placement ray: a dock section's edge (<see cref="KwPierLadderEdge.Dock"/>), a game floor's edge
    /// (<see cref="KwPierLadderEdge.Floor"/>, unless a hatch is aimed through), or a dock ladder already there (refused);
    /// anything else is the kit's ladder tool's (null). Then the rules: no other dock ladder within <see cref="Apart"/>, within the game's building reach, not
    /// in a no-build place or a ward the player may not use, and water under the edge. Its length runs from the deck's top
    /// down to <see cref="Below"/> under the water's surface there (the game's water, waves and all as they are at that
    /// moment, never above the calm sea's), or to the sea floor where that is shallower, to <see cref="Step"/>;
    /// <see cref="KwPierLadderSpec.MinLength"/> to <see cref="KwPierLadderSpec.MaxLength"/>. Reads only.
    /// </summary>
    public static class KwPierLadderAim
    {
        public const float Apart = 0.8f, Below = 0.4f, Step = 0.05f;
        private const float Reach = 50f;
        private static readonly int Ground = LayerMask.GetMask("terrain", "static_solid", "Default", "Default_small");

        /// <summary>The site the aim gives (flash: a ward refusing it flashes, the click's); null when it is not on a dock, a dock ladder or a game floor.</summary>
        public static KwPierLadderSite? Of(Player player, bool flash)
        {
            Transform eye = GameCamera.instance.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, Reach, player.m_placeRayMask, QueryTriggerInteraction.Ignore))
                return null;
            Piece? piece = hit.collider.GetComponentInParent<Piece>();
            KwPierLadderSite? site = piece != null ? Site(player, piece, hit) : null;
            if (site != null && site.Why == null)
                site.Why = Rules(player, site, flash) ?? Drop(site);
            return site;
        }

        // A dock, a dock ladder, or a game floor (unless the kit's aim sees a hatch through which it was hit: that is the
        // kit's hatch ladder).
        private static KwPierLadderSite? Site(Player player, Piece piece, RaycastHit hit)
        {
            if (piece.GetComponent<KwPierLadder>() != null)
                return KwPierLadderSite.Refused("A dock ladder hangs here already (the hammer takes it down)");
            KwPier? pier = piece.GetComponent<KwPier>();
            if (pier != null)
                return KwPierLadderEdge.Dock(pier, hit.point);
            return KwLadderAim.Of(player).Hatch == null ? KwPierLadderEdge.Floor(piece, hit) : null;
        }

        // Another ladder, the reach, a no-build place, a ward; null when none refuses it.
        private static string? Rules(Player player, KwPierLadderSite site, bool flash)
        {
            foreach (KwPierLadder ladder in KwPierLadder.Loaded)
            {
                if (ladder != null && (ladder.transform.position - site.Top).sqrMagnitude < Apart * Apart)
                    return "Another ladder hangs there";
            }
            Piece? tool = player.GetSelectedPiece();
            if (Vector3.Distance(player.m_eye.position, site.Top) > player.m_maxPlaceDistance + (tool != null ? tool.m_extraPlacementDistance : 0))
                return "Too far away";
            if (Location.IsInsideNoBuildLocation(site.Top))
                return "$msg_nobuildzone";
            return PrivateArea.CheckAccess(site.Top, 0f, flash, false) ? null : "$msg_privatezone";
        }

        // Its length down to under the water's surface, or to the sea floor; why not when there is no water there or the
        // drop is too short or too long.
        private static string? Drop(KwPierLadderSite site)
        {
            Vector3 foot = site.Top + site.Outward * KwPierLadderShape.Out;
            float sea = KwPierAim.Sea;
            Vector3 probe = new Vector3(foot.x, sea, foot.z);
            float calm = Floating.GetLiquidLevel(probe, 0f, LiquidType.Water), waves = Floating.GetLiquidLevel(probe, 1f, LiquidType.Water);
            float floor = Physics.Raycast(foot + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, KwPierLadderSpec.MaxLength + 2f, Ground) ? hit.point.y : float.MinValue;
            if (calm < -1000f || floor >= calm - 0.05f)
                return "No water under this edge for a ladder";
            float bottom = Mathf.Max(Mathf.Min(calm, waves) - Below, floor);
            (site.Length, site.Grounded) = (Mathf.Ceil((site.Top.y - bottom) / Step - 0.01f) * Step, floor > bottom - 0.01f);
            if (site.Length < KwPierLadderSpec.MinLength)
                return "The water is too close under this edge for a ladder";
            return site.Length > KwPierLadderSpec.MaxLength ? $"The water is too far down here ({KwPierLadderSpec.MaxLength:0} m of ladder at most)" : null;
        }
    }
}
