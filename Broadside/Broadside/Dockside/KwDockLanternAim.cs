using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a dock lantern's aim means (DOCKS.md, "Dock lanterns"), on the aiming player's game, along the game's placement
    /// ray: a dock section's edge (<see cref="KwDockLanternEdge.Dock"/>) or a game floor's edge over the water
    /// (<see cref="KwDockLanternEdge.Floor"/>); anything else is refused (<see cref="Hint"/>: the lantern pole stands
    /// anywhere). Then the rules: within the game's building reach, not in a no-build place or a ward the player may not use,
    /// no other dock lantern within <see cref="Apart"/> and no dock ladder within <see cref="LadderApart"/> of its edge point,
    /// and its post down to the ground (<see cref="KwDockLanternEdge.Post"/>). The Auto-decorate tool asks the same at a point
    /// on a dock's edge (<see cref="At"/>). Reads only.
    /// </summary>
    public static class KwDockLanternAim
    {
        public const string Hint = "A dock lantern stands beside a dock: aim at a dock's side (the lantern pole stands anywhere)";
        public const float Apart = 1f, LadderApart = 0.6f;
        private const float Reach = 50f, Level = 0.3f, Over = 0.5f;
        private static readonly int Pieces = LayerMask.GetMask("piece");

        /// <summary>The site the player's aim gives (flash: a ward refusing it flashes, the click's); refused, with why, off a dock's side.</summary>
        public static KwDockLanternSite Of(Player player, bool flash)
        {
            KwDockLanternSite site = Site(player);
            if (site.Why == null)
                site.Why = Rules(player, site, flash) ?? Taken(site) ?? KwDockLanternEdge.Post(site);
            return site;
        }

        private static KwDockLanternSite Site(Player player)
        {
            Transform eye = GameCamera.instance.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, Reach, player.m_placeRayMask, QueryTriggerInteraction.Ignore))
                return KwDockLanternSite.Refused(Hint);
            Piece? piece = hit.collider.GetComponentInParent<Piece>();
            if (piece != null && piece.GetComponent<KwDockLanternPiece>() != null)
                return KwDockLanternSite.Refused("A dock lantern stands here already (the hammer takes it down)");
            KwPier? pier = piece != null ? piece.GetComponent<KwPier>() : null;
            if (pier != null)
                return KwDockLanternEdge.Dock(pier, hit.point);
            return (piece != null ? KwDockLanternEdge.Floor(piece, hit) : null) ?? KwDockLanternSite.Refused(Hint);
        }

        /// <summary>
        /// For the Auto-decorate tool: the site at a point on a dock's edge row (a dock section's deck there, else a game floor
        /// under it), with its post; null when there is no dock or floor there, or the lantern may not stand there.
        /// </summary>
        public static KwDockLanternSite? At(Vector3 point)
        {
            KwDockLanternSite? site = null;
            foreach (KwPierDeck deck in KwPierDeck.Near(point, 0.05f))
            {
                KwPier? pier = deck.Piece.GetComponent<KwPier>();
                if (pier != null && Mathf.Abs(point.y - deck.Top) <= Level)
                    site = KwDockLanternEdge.Dock(pier, point);
            }
            if (site == null && Physics.Raycast(point + Vector3.up * Over, Vector3.down, out RaycastHit hit, Over + Level, Pieces, QueryTriggerInteraction.Ignore))
            {
                Piece? piece = hit.collider.GetComponentInParent<Piece>();
                site = piece != null && piece.GetComponent<KwPier>() == null ? KwDockLanternEdge.Floor(piece, hit) : null;
            }
            if (site != null && site.Why == null)
                site.Why = Taken(site) ?? KwDockLanternEdge.Post(site);
            return site != null && site.Why == null ? site : null;
        }

        // The reach, a no-build place, a ward; null when none refuses it.
        private static string? Rules(Player player, KwDockLanternSite site, bool flash)
        {
            Piece? held = player.GetSelectedPiece();
            if (Vector3.Distance(player.m_eye.position, site.Edge) > player.m_maxPlaceDistance + (held != null ? held.m_extraPlacementDistance : 0))
                return "Too far away";
            if (Location.IsInsideNoBuildLocation(site.Origin))
                return "$msg_nobuildzone";
            return PrivateArea.CheckAccess(site.Origin, 0f, flash, false) ? null : "$msg_privatezone";
        }

        // Another dock lantern standing there, or a dock ladder hanging there; null when neither.
        private static string? Taken(KwDockLanternSite site)
        {
            foreach (KwDockLanternPiece lantern in KwDockLanternPiece.Loaded)
            {
                if (lantern != null && (lantern.transform.position - site.Origin).sqrMagnitude < Apart * Apart)
                    return "Another dock lantern stands there";
            }
            foreach (KwPierLadder ladder in KwPierLadder.Loaded)
            {
                if (ladder != null && (ladder.transform.position - site.Edge).sqrMagnitude < LadderApart * LadderApart)
                    return "A dock ladder hangs there";
            }
            return null;
        }
    }
}
