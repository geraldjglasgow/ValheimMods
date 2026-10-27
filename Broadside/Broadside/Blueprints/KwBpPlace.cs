using System;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A click with the hammer on a saved ship (<c>KW_bp_</c>, <see cref="KwBpEntries"/>): the entry is never placed
    /// itself; the click launches the whole ship where its ghost stands on the water, turned as the ghost is, a ship that
    /// floats and sails (<see cref="KwShipLaunch"/>). Before the click the game has checked the workbench in range and the
    /// requirements the piece shows; the rest of the cost (<see cref="KwBpEntry.Rest"/>) is checked here and taken from the
    /// player as the ship is made, and the game then takes the requirements it showed, as for any piece it places (none
    /// with the world's free build key; with the no-cost cheat nothing is checked).
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class KwBpPlace
    {
        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (piece == null || !piece.name.StartsWith(KwBlueprintStation.EntryPrefix, StringComparison.Ordinal))
                return true;
            __result = Guard.Run<Player, Piece, bool>("Broadside ship build", Build, __instance, piece);
            return false;
        }

        // True when the ship was made (the game then takes the shown cost).
        private static bool Build(Player player, Piece piece)
        {
            player.UpdatePlacementGhost(true);
            GameObject? ghost = player.m_placementGhost;
            KwBpEntry? entry = KwBpEntries.Of(piece);
            if (entry == null || ghost == null || !ghost.activeSelf || player.m_placementStatus != Player.PlacementStatus.Valid)
                return Say(player, KwBpWater.Reason ?? "$msg_invalidplacement");
            string? missing = player.NoCostCheat() ? null : KwBpCost.Missing(player, entry.Rest);
            if (missing != null)
                return Say(player, $"{entry.Blueprint.Name} also needs {missing}");
            Quaternion turn = ghost.transform.rotation;
            if (KwShipLaunch.Spawn(entry.Blueprint, ghost.transform.position - turn * entry.Anchor, turn) == null)
                return Say(player, "The ship could not be made (the log says why)");
            if (!ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey()))
                KwBpCost.Take(player, entry.Rest);
            Say(player, $"{entry.Blueprint.Name} is on the water");
            return true;
        }

        // The player told why, and nothing placed.
        private static bool Say(Player player, string text)
        {
            player.Message(MessageHud.MessageType.Center, text);
            return false;
        }
    }
}
