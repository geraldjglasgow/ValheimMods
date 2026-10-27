using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The local player's controls while he hauls a ship cannon by its rear (<see cref="KwArmTow.Mine"/>; a gunner's are
    /// the MannedWeapons library's): he cannot run or jump, and walks (the game's own walk, its animation, no faster than
    /// the carriage's <see cref="KwShipSpec.Pace"/>) for as long as he hauls, his feet then steered by the hitch (stepping
    /// in behind its rear, held back from a way it cannot follow him: <see cref="KwTowHitch.Steer"/>); his walk setting
    /// comes back when he lets go (<see cref="KwTowHitch"/>). Only the local player's controls come through here
    /// (PlayerController).
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    public static class KwArmControls
    {
        [HarmonyPrefix]
        private static void Prefix(ref bool run, ref bool jump)
        {
            if (KwArmTow.Mine != null)
                (run, jump) = (false, false);
        }

        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (KwArmTow.Mine == null)
                return;
            Hauling(__instance, KwArmTow.Mine.Spec.Pace);
            Guard.Run("Broadside cannon tow", static p => KwArmTow.Mine?.Hitch.Steer(p), __instance);
        }

        // The hauler walks, no faster than the carriage's pace (metres a second).
        private static void Hauling(Player player, float pace)
        {
            player.SetWalk(true);
            float most = pace > 0f ? pace / Mathf.Max(0.1f, player.m_walkSpeed) : 1f;
            if (most < 1f && player.m_moveDir.sqrMagnitude > most * most)
                player.m_moveDir = Vector3.ClampMagnitude(player.m_moveDir, most);
        }
    }
}
