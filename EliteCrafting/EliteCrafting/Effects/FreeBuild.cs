using HarmonyLib;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// Masterbuilder (<c>free_build</c>): while this hammer or hoe is in hand, building ignores the crafting-station
    /// requirement, as the game's own "no workbench" world setting does - for this player and this tool only. Item-local:
    /// the flag on the tool in the right hand (where the game keeps the build tool).
    /// <para>
    /// The game asks <c>Player.HaveRequirements(piece, CanBuild)</c> before placing a piece (and to colour the build
    /// menu); a missing station makes it false. When it is false for a piece that needs a station, the check is run
    /// again with the piece's station requirement set aside, so the materials, the DLC and every other mod's part of
    /// that check (OpenKeep's materials from chests) still count; the requirement is put back at once. Removing a piece
    /// skips the station check the same way. The builder's own client: placement and removal are decided there.
    /// </para>
    /// </summary>
    internal static class FreeBuild
    {
        /// <summary>Whether this player is the local player with a Masterbuilder tool in hand.</summary>
        public static bool Active(Player player)
        {
            if (!ReferenceEquals(player, Player.m_localPlayer))
            {
                return false;
            }
            ItemLocalSums? sums = ItemLocalCache.Get(player.RightItem);
            return sums != null && sums.Get(EffectKind.FreeBuild) > 0f;
        }

        /// <summary>The game's own check again, without the station.</summary>
        public static bool HaveWithoutStation(Player player, Piece piece, Player.RequirementMode mode)
        {
            CraftingStation station = piece.m_craftingStation;
            piece.m_craftingStation = null;
            try
            {
                return player.HaveRequirements(piece, mode);
            }
            finally
            {
                piece.m_craftingStation = station;
            }
        }
    }

    [HarmonyPatch]
    internal static class FreeBuildPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Piece), typeof(Player.RequirementMode) })]
        private static void Requirements(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
        {
            if (__result || piece == null || piece.m_craftingStation == null || mode != Player.RequirementMode.CanBuild)
            {
                return;
            }
            if (FreeBuild.Active(__instance))
            {
                __result = FreeBuild.HaveWithoutStation(__instance, piece, mode);
            }
        }

        // The removal check is only the station check: with the tool in hand, removing is allowed.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.CheckCanRemovePiece))]
        private static bool Removal(Player __instance, ref bool __result)
        {
            if (!FreeBuild.Active(__instance))
            {
                return true;
            }
            __result = true;
            return false;
        }
    }
}
