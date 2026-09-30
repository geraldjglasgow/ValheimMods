using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The called wind itself. The game works out the wind on every machine from the world time (EnvMan.UpdateWind, each
    /// physics step) and eases into a new target over a few seconds (SetTargetWind); a ship's sail force is worked out
    /// with the wind on the machine that owns the ship, which the game keeps with a player aboard. So, like the game's
    /// own Moder power, the called wind is applied on every client whose player is aboard that ship: the owner's sail,
    /// and the sails, cloth, trees and wind indicator everyone aboard sees, all agree. It wins over Moder's tailwind
    /// while it blows. The game's own debug wind (the "wind" console command) and the wind that pushes players back at
    /// the edge of the world stay as they are. Players ashore and on other ships keep the weather's wind.
    /// </summary>
    public static class WindCallWind
    {
        [HarmonyPatch(typeof(EnvMan), nameof(EnvMan.SetTargetWind))]
        private static class Target
        {
            [HarmonyPrefix]
            private static void Prefix(EnvMan __instance, ref Vector3 dir)
            {
                if (!__instance.m_debugWind && Called(__instance, out Vector3 called))
                    dir = called;
            }
        }

        private static bool Called(EnvMan env, out Vector3 direction)
        {
            direction = Vector3.zero;
            Player player = Player.m_localPlayer;
            if (player == null || !SailingSkill.Active || AtWorldEdge(env, player))
                return false;
            Ship ship = Ship.GetLocalShip();
            return ship != null && WindCallState.TryRead(ship, out direction);
        }

        /// <summary>The game's own test in UpdateWind for the wind that blows players back from the edge of the world.</summary>
        private static bool AtWorldEdge(EnvMan env, Player player) =>
            Utils.LengthXZ(player.transform.position) > 10500f - env.m_edgeOfWorldWidth;
    }
}
