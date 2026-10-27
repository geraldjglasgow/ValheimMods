using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Who may break up a launched ship with the hammer's remove (<see cref="KwShipBreak"/>): the game's own checks for its
    /// ships, in its own order (the piece removable and outside a no-build location, the ward's access, the ship's station,
    /// the karve's workbench, in range unless the no-cost cheat or the world's no-workbench key says otherwise), and
    /// nobody aboard (<see cref="Aboard"/>), the remover included, so nobody is dropped in the sea. The hover asks all of
    /// it, the click only whether anyone is aboard (the game says its own words for the rest), and the ship's owner asks
    /// that again before it breaks the ship up.
    /// </summary>
    public static class KwShipBreakRules
    {
        public const string Everyone = "Everyone must leave the ship first";
        private const float Margin = 1f, Above = 25f;

        /// <summary>Why the player may not break up the ship now, in words; null when they may.</summary>
        public static string? Why(Player player, Piece piece, Ship ship, KwBpHull? hull)
        {
            Vector3 at = piece.transform.position;
            if (!piece.m_canBeRemoved || Location.IsInsideNoBuildLocation(at))
                return "The ship can't be broken up here";
            if (!PrivateArea.CheckAccess(at, 0f, false))
                return "A ward keeps you from breaking up the ship";
            if (!Station(player, piece))
                return "A workbench must be near to break up the ship";
            return Aboard(ship, hull) ? Everyone : null;
        }

        // The game's station check for removing a piece: the ship's (the karve's workbench) in range of the player.
        private static bool Station(Player player, Piece piece) =>
            player.m_noPlacementCost || piece.m_craftingStation == null || ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench)
            || CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, player.transform.position) != null;

        /// <summary>
        /// Whether anyone is aboard: in the game's onboard trigger (round the hull to 25 m over its sheer, KwShipFittings.Aloft,
        /// <see cref="KwShipFittings"/>), or over the hull's plan higher up (on a cabin's roof, up a mast, <see cref="Over"/>),
        /// as this game sees the players.
        /// </summary>
        public static bool Aboard(Ship ship, KwBpHull? hull)
        {
            if (ship.m_players.Count > 0)
                return true;
            if (hull == null)
                return false;
            foreach (Player player in Player.GetAllPlayers())
            {
                if (player != null && Over(ship.transform.InverseTransformPoint(player.transform.position), hull))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Whether a point in the ship's own frame is over its hull's plan above the onboard trigger's reach (which goes
        /// 2 m over the sheer): from the sheer up to above its masts, 25 m over it. Swimmers beside the hull are left to
        /// the trigger, as the game leaves them for its own ships.
        /// </summary>
        public static bool Over(Vector3 local, KwBpHull hull) =>
            Mathf.Abs(local.x) <= hull.Beam / 2f + Margin && local.z >= hull.Aft - Margin && local.z <= hull.Fore + Margin
            && local.y >= hull.Keel + hull.Depth && local.y <= hull.Keel + hull.Depth + Above;
    }
}
