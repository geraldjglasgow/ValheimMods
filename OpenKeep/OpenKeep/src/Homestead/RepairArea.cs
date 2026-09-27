using System.Collections.Generic;
using OpenKeep.Core;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// One area repair on the repairing player's client, after the game repaired the hovered piece: every piece touching
    /// it (<see cref="RepairNeighbours"/>, nearest first) that passes the hand repair's checks (<see cref="RepairChecks"/>)
    /// gets the game's own <c>WearNTear.Repair</c>, which refuses an undamaged piece and sends <c>RPC_Repair</c> to the
    /// piece's ZDO owner (handled at once when this client owns it, else routed through the server to the owning peer). The owner sets the
    /// full health in the ZDO and tells every client (<c>RPC_HealthChanged</c>), so everyone sees the worn look go. Each
    /// repaired neighbour plays its own place effect here, as the game does for the hovered piece. At most
    /// <see cref="MaxRepairs"/> neighbours per swing; they cost nothing, the swing was charged by the game.
    /// </summary>
    public static class RepairArea
    {
        public const int MaxRepairs = 64;

        /// <summary>The piece <c>Player.Repair</c> works on: the hovered piece's own <c>WearNTear</c>.</summary>
        public static WearNTear Hovered(Player player)
        {
            Piece piece = player.GetHoveringPiece();
            return piece != null ? piece.GetComponent<WearNTear>() : null;
        }

        public static void Run(Player player, WearNTear centre)
        {
            List<WearNTear> near = RepairNeighbours.Around(centre);
            RepairTally tally = new RepairTally(near.Count);
            foreach (WearNTear wear in near)
            {
                if (tally.Repaired >= MaxRepairs)
                {
                    tally.Capped = true;
                    break;
                }
                tally.Count(RepairOne(player, wear));
            }
            Report(centre, tally);
        }

        private static RepairOutcome RepairOne(Player player, WearNTear wear)
        {
            Piece piece = wear.m_piece;
            if (piece == null || wear.m_nview == null)
                return RepairOutcome.NotAPiece;
            if (!RepairChecks.StationInRange(player, piece))
                return RepairOutcome.NoStation;
            if (!RepairChecks.WardAllows(piece))
                return RepairOutcome.Warded;
            if (!wear.Repair())
                return RepairOutcome.Undamaged;
            piece.m_placeEffect?.Create(piece.transform.position, piece.transform.rotation, null, 1f, -1, player.GetZDOID());
            return RepairOutcome.Repaired;
        }

        private static void Report(WearNTear centre, RepairTally tally)
        {
            if (tally.Repaired == 1)
                Messages.TopLeft(RepairFeature.RepairedOne);
            else if (tally.Repaired > 1)
                Messages.TopLeft(string.Format(Language.Localize(RepairFeature.RepairedMany), tally.Repaired));
            if (tally.Touching > 0)
                Plugin.Log.LogInfo($"OpenKeep: area repair around {Utils.GetPrefabName(centre.gameObject)}: {tally}");
        }
    }
}
