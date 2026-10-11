using System;
using HarmonyLib;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Puts the raid's line in the game's own event bar (<see cref="RaidHudLine"/>). <c>Hud.UpdateEvent</c> runs every
    /// frame with the local player; while the player is at one of the mod's raids the line is the raid's and the game's
    /// own step is skipped (it would hide the bar again each frame), otherwise the game's step runs untouched. One test
    /// of a short list when no raid is loaded. Never throws into the HUD: a failure leaves the bar to the game.
    /// </summary>
    [HarmonyPatch(typeof(Hud), "UpdateEvent")]
    internal static class RaidHudPatch
    {
        private static bool Prefix(Hud __instance, Player player)
        {
            try
            {
                return !RaidHudLine.Draw(__instance, player);
            }
            catch (Exception e)
            {
                RaidHudLine.Fail(e);
                return true;
            }
        }
    }
}
