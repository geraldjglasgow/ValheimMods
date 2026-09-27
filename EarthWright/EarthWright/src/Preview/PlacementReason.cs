using EarthWright.Core;

namespace EarthWright.Preview
{
    /// <summary>
    /// The game's own placement check of the ghost (the status <c>Player.TryPlacePiece</c> refuses a click with: no-build
    /// zone, ward, dungeon, needs cultivated ground, ...), reported as a preview reason under the key "placement" with the
    /// game's own message, so the outline turns red and the HUD says why before the player clicks. Brush entries only:
    /// special entries never reach the game's check.
    /// </summary>
    internal static class PlacementReason
    {
        private const string Key = "placement";

        private static bool reported;

        /// <summary>Reports the game's refusal for the player's ghost now; true when the click would be refused.</summary>
        public static bool Update(Player player)
        {
            string reason = Message(player.m_placementStatus);
            PreviewStatus.Report(Key, reason);
            reported = reason != null;
            return reported;
        }

        public static void Clear()
        {
            if (!reported)
                return;
            reported = false;
            PreviewStatus.Report(Key, null);
        }

        /// <summary>The game's message token for a status (as <c>Player.TryPlacePiece</c> shows it), null when valid.</summary>
        private static string Message(Player.PlacementStatus status)
        {
            switch (status)
            {
                case Player.PlacementStatus.Valid: return null;
                case Player.PlacementStatus.NoBuildZone: return "$msg_nobuildzone";
                case Player.PlacementStatus.BlockedbyPlayer: return "$msg_blocked";
                case Player.PlacementStatus.PrivateZone: return "$msg_privatezone";
                case Player.PlacementStatus.MoreSpace: return "$msg_needspace";
                case Player.PlacementStatus.WrongBiome: return "$msg_wrongbiome";
                case Player.PlacementStatus.NeedCultivated: return "$msg_needcultivated";
                case Player.PlacementStatus.NeedDirt: return "$msg_needdirt";
                case Player.PlacementStatus.NotInDungeon: return "$msg_notindungeon";
                case Player.PlacementStatus.DeepSnow: return "$msg_snowtoodeep";
                case Player.PlacementStatus.NoSnow: return "$msg_nosnow";
                default: return "$msg_invalidplacement";
            }
        }
    }
}
