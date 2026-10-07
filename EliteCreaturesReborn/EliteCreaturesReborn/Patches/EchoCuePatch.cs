using System;
using EliteCreaturesReborn.Aspects;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Every animation cue a creature's owner sends (a swing, a stagger, a taunt), before the game sends it to every
    /// machine: an Echoing boss's go on its tape (<see cref="EchoTapes.Cue"/>) for its echo to play `delay` seconds later.
    /// With no boss recorded here it is one count check. A failure is reported, never rethrown, so the cue is always sent.
    /// </summary>
    [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetTrigger))]
    public static class EchoCuePatch
    {
        private static void Prefix(ZSyncAnimation __instance, string name)
        {
            if (!EchoTapes.Any)
            {
                return;
            }
            try
            {
                EchoTapes.Cue(__instance, name);
            }
            catch (Exception e)
            {
                Guard.Report(e, "ZSyncAnimation.SetTrigger echoing");
            }
        }
    }
}
