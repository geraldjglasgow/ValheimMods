using System;
using EliteCreaturesReborn.Aspects;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The one patch on <c>Character.UpdateMotion</c>, the start of a character's movement step on its owner, run every
    /// physics step for every character. An echo's step is skipped: it walks, turns and flies only as its boss did, put
    /// in place by <see cref="EchoDriver"/>, so the game's walking, falling, gravity and friction never move it. For
    /// everything else, before the game blends its stored swim velocity toward the swimmer's intent, a Gravitic pull's
    /// last push is taken back out of it (<see cref="GraviticPull.Repay"/>), so in water the pull never outlasts itself.
    /// With no echo loaded and nothing owed it is two checks; a failure is reported, never rethrown, and lets the step run.
    /// </summary>
    [HarmonyPatch(typeof(Character), "UpdateMotion")]
    public static class MotionPatch
    {
        private static bool Prefix(Character __instance)
        {
            if (EchoLink.Any && EchoLink.IsEcho(__instance))
            {
                return false;
            }
            if (!GraviticPull.Owes)
            {
                return true;
            }
            try
            {
                GraviticPull.Repay(__instance);
            }
            catch (Exception e)
            {
                Guard.Report(e, "Character.UpdateMotion gravitic");
            }
            return true;
        }
    }
}
