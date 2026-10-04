using System;
using EliteCreaturesReborn.Mutations;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Corrodent's armour wear (<see cref="CorrodentWear"/>), where the game wears a player's armour under a hit: inside
    /// the hit, on the hit player's own machine, the one place their gear lives. The prefix notes the worn pieces'
    /// durability only for a hit by a Corrodent creature and never throws, so it can never stop the hit; the postfix
    /// multiplies whatever the game took. A failure there is reported and swallowed: the game's own wear has happened
    /// and the hit goes on.
    /// </summary>
    [HarmonyPatch(typeof(Player), "DamageArmorDurability")]
    public static class CorrodentPatch
    {
        private static void Prefix(Player __instance, HitData hit, out CorrodentWear.Before __state)
        {
            __state = default;
            try
            {
                if (hit != null)
                {
                    __state = CorrodentWear.Capture(__instance, hit);
                }
            }
            catch (Exception e)
            {
                Guard.Report(e, "Player.DamageArmorDurability corrodent capture");
            }
        }

        private static void Postfix(Player __instance, CorrodentWear.Before __state)
        {
            if (__state.Active)
            {
                SafeCall.Run("Player.DamageArmorDurability corrodent", () => CorrodentWear.Apply(__instance, __state));
            }
        }
    }
}
