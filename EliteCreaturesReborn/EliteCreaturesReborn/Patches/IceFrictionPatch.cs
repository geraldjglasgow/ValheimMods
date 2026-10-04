using System;
using EliteCreaturesReborn.Mutations;
using HarmonyLib;
using PatchGuard;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Character.UpdateBodyFriction sets how hard a body's feet hold the ground, each physics step (and as it jumps).
    /// While this machine's player stands on a Frostbound patch, alive and on the ground, their feet hold only the
    /// grip's share of it (<see cref="IceFooting.Loosen"/>), so friction does not stop the slide the movement patch
    /// lets run (<see cref="IceSlipPatch"/>). Anyone else, or anyone while nobody here is on ice, costs one float
    /// check. A failure is reported and leaves the game's friction as it was.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.UpdateBodyFriction))]
    public static class IceFrictionPatch
    {
        private static void Postfix(Character __instance)
        {
            if (!IceFooting.Slipping || !ReferenceEquals(__instance, Player.m_localPlayer))
            {
                return;
            }
            try
            {
                if (!__instance.IsDead() && __instance.IsOnGround() && __instance.m_collider != null)
                {
                    IceFooting.Loosen(__instance.m_collider.material);
                }
            }
            catch (Exception e)
            {
                Guard.Report(e, "Character.UpdateBodyFriction ice");
            }
        }
    }
}
