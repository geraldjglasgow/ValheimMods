using System;
using EliteCreaturesReborn.Arena.Trial;
using HarmonyLib;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Puts the raid's line in the game's own event bar (<see cref="RaidHudLine"/>). <c>Hud.UpdateEvent</c> runs every
    /// frame with the local player; while the player is at one of the mod's raids the line is the raid's and the game's
    /// own step is skipped (it would hide the bar again each frame), otherwise the game's step runs untouched. One test
    /// of a short list when no raid is loaded. Never throws into the HUD: a failure leaves the bar to the game. The one
    /// hook on this per-frame method also draws Hel's Trial's line, asked first: a player in the hall is at no raid
    /// (<see cref="ArenaHudLine"/>).
    /// </summary>
    [HarmonyPatch(typeof(Hud), "UpdateEvent")]
    internal static class RaidHudPatch
    {
        private static bool Prefix(Hud __instance, Player player)
        {
            if (TrialLine(__instance, player))
            {
                return false;
            }
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

        // The trial's line, on its own guard: its failure turns off only its own line.
        private static bool TrialLine(Hud hud, Player player)
        {
            try
            {
                return ArenaHudLine.Draw(hud, player);
            }
            catch (Exception e)
            {
                ArenaHudLine.Fail(e);
                return false;
            }
        }
    }
}
