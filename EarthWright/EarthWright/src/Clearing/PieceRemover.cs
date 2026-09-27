using System.Collections.Generic;
using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// <c>ew pieces [radius] [refund]</c> (admin): removes every player-built piece around the admin, wards included.
    /// Pieces with WearNTear are removed through the game's own remove RPC, which their owner handles (containers still
    /// spill their contents, beds lose their spawn point); "refund" also drops the building materials. Other pieces
    /// are claimed and destroyed.
    /// </summary>
    public static class PieceRemover
    {
        private const float DefaultRadius = 10f;

        public static void Run(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                args.Context.AddString("EarthWright: join a world first.");
                return;
            }
            float radius = ClearingSettings.CommandRadius(CommandInput.Number(args, 2, DefaultRadius), admin: true);
            bool refund = CommandInput.HasWord(args, "refund");
            int removed = 0;
            foreach (Piece piece in Collect(player.transform.position, radius))
            {
                if (Safe.Call("EarthWright ew pieces", () => Remove(piece, refund), false))
                    removed++;
            }
            string materials = refund ? "their materials were dropped" : "nothing was refunded";
            args.Context.AddString($"EarthWright: removed {removed} player-built pieces within {ClearingWords.Metres(radius)} m; {materials}.");
        }

        /// <summary>The player-built pieces around the point, collected first because removing them changes the game's list.</summary>
        private static List<Piece> Collect(Vector3 center, float radius)
        {
            List<Piece> found = new List<Piece>();
            bool interior = Character.InInterior(center);
            foreach (Piece piece in Piece.s_allPieces)
            {
                if (piece == null || !piece.IsPlacedByPlayer() || piece.m_nview == null || !piece.m_nview.IsValid())
                    continue;
                Vector3 p = piece.transform.position;
                float dx = p.x - center.x;
                float dz = p.z - center.z;
                if (dx * dx + dz * dz <= radius * radius && Character.InInterior(p) == interior)
                    found.Add(piece);
            }
            return found;
        }

        private static bool Remove(Piece piece, bool refund)
        {
            if (piece == null || piece.m_nview == null || !piece.m_nview.IsValid())
                return false;
            WearNTear wear = piece.GetComponent<WearNTear>();
            if (wear != null)
            {
                wear.Remove(blockDrop: !refund);
                return true;
            }
            piece.m_nview.ClaimOwnership();
            if (refund)
                piece.DropResources();
            ClearExecutor.Remove(piece.m_nview);
            return true;
        }
    }
}
