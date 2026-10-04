using AreaLoading;
using HarmonyLib;
using Wayfare.Core;

namespace Wayfare.QuickJumps
{
    /// <summary>Entry point of the quick jumps (section "Jump Speed"): jump time by distance (<see cref="JumpTiming"/>),
    /// the teleport screen only when there is something to load (<see cref="JumpScreen"/>), and Quick Area Loading for
    /// the land and objects around the target, the AreaLoading library's, hurried while the local player's jump loads
    /// behind the teleport screen. Moved here from OpenKeep's Homestead section on 2026-10-04 (Wayfare is the teleport
    /// mod); OpenKeep keeps the same library for respawns, so with both installed each hurries for its own reason.</summary>
    public static class JumpSpeed
    {
        public static void Install(Harmony harmony)
        {
            AreaLoader.Install(harmony, text => Plugin.Log.LogInfo("Wayfare: " + text));
            AreaLoader.When(LoadingLand);
        }

        /// <summary>The local player's jump waits for its area behind the teleport screen.</summary>
        private static bool LoadingLand()
        {
            Player player = Player.m_localPlayer;
            return WayfareConfig.Enabled.Value && WayfareConfig.QuickAreaLoading.Value && player != null
                && player.IsTeleporting() && !JumpScreen.Clear(player);
        }
    }
}
