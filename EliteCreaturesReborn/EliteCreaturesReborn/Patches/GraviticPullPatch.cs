using System;
using EliteCreaturesReborn.Aspects;
using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The one place the game adds a knockback to a character's movement, run every physics step on the character's
    /// owner, on foot and swimming alike, just before the velocity is applied. While a Gravitic boss is pulling this
    /// machine's own player, the pull is added here (<see cref="GraviticPull"/>), so the game carries it out as its own
    /// intent rather than correcting it away the next step. Any other character, or any step with no pull running, is
    /// a single bool check. It runs inside the game's character loop, so a failure is reported and never rethrown.
    /// </summary>
    [HarmonyPatch(typeof(Character), "AddPushbackForce")]
    public static class GraviticPullPatch
    {
        private static void Postfix(Character __instance, ref Vector3 velocity)
        {
            if (!GraviticPull.Active || !(__instance is Player player) || player != Player.m_localPlayer)
            {
                return;
            }
            try
            {
                GraviticPull.Steer(player, ref velocity);
            }
            catch (Exception e)
            {
                Guard.Report(e, "Character.AddPushbackForce gravitic");
            }
        }
    }
}
