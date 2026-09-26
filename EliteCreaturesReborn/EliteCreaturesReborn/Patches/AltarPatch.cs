using System;
using EliteCreaturesReborn.Aspects;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Gives every boss altar its <see cref="AltarAspect"/> as the bowl starts, on every machine. The bowl's network view
    /// is found in its own Start, so this runs after it; an altar that summons an item rather than a boss is left alone.
    /// </summary>
    [HarmonyPatch(typeof(OfferingBowl), "Start")]
    public static class AltarStartPatch
    {
        private static void Postfix(OfferingBowl __instance) =>
            Guard.Run("OfferingBowl.Start aspect", () => AltarAspect.Attach(__instance));
    }

    /// <summary>Appends the altar's current aspect, what it pays and when it shifts, below the bowl's own hover text.</summary>
    [HarmonyPatch(typeof(OfferingBowl), nameof(OfferingBowl.GetHoverText))]
    public static class AltarHoverPatch
    {
        private static void Postfix(OfferingBowl __instance, ref string __result)
        {
            string current = __result;
            __result = Guard.Run("OfferingBowl.GetHoverText aspect", () => current + Lines(__instance));
        }

        private static string Lines(OfferingBowl bowl)
        {
            AltarAspect altar = bowl.GetComponent<AltarAspect>();
            return altar != null ? altar.HoverLines() : "";
        }
    }

    /// <summary>
    /// The offering: the game has accepted it and is about to count down to the boss, on the bowl's owner. The aspect on
    /// the bowl right now is the fight - locked here, before the countdown gives the altar a chance to shift.
    /// </summary>
    [HarmonyPatch(typeof(OfferingBowl), "SpawnBoss")]
    public static class AltarLockPatch
    {
        private static void Prefix(OfferingBowl __instance) =>
            AltarSafety.Run("OfferingBowl.SpawnBoss aspect", () => Lock(__instance));

        private static void Lock(OfferingBowl bowl)
        {
            AltarAspect altar = bowl.GetComponent<AltarAspect>();
            if (altar != null)
            {
                altar.Lock();
            }
        }
    }

    /// <summary>
    /// The countdown's end, where the game instantiates the boss: the locked aspect is held for exactly this one call,
    /// and the finalizer lets it go whatever happens, so no later spawn can inherit it.
    /// </summary>
    [HarmonyPatch(typeof(OfferingBowl), "DelayedSpawnBoss")]
    public static class AltarSpawnPatch
    {
        private static void Prefix(OfferingBowl __instance) =>
            AltarSafety.Run("OfferingBowl.DelayedSpawnBoss aspect", () => Hold(__instance));

        private static void Hold(OfferingBowl bowl)
        {
            AltarAspect altar = bowl.GetComponent<AltarAspect>();
            AltarSummon.Begin(altar != null ? altar.TakeLocked() : null);
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            AltarSummon.End();
            return __exception;
        }
    }

    /// <summary>
    /// The altar prefixes run just before the game spends the offering and spawns the boss, so they must never throw
    /// into it: a failure is reported under the mod's name and swallowed, and the boss simply rolls its own aspect.
    /// </summary>
    internal static class AltarSafety
    {
        public static void Run(string context, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Guard.Report(e, context);
            }
        }
    }
}
