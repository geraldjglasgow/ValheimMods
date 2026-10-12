using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Gallop, on the mount's owner, which simulates its movement: Character.UpdateWalking (owner only) sets a running
    /// creature's speed to m_runSpeed x GetRunSpeedFactor, and Character.GetRunSpeedFactor gives a ridden mount the game's
    /// 1 + 0.25 x the rider's Ride factor. A postfix multiplies that by 1 + <see cref="FasterAt100"/> x the same factor
    /// (<see cref="MountRider"/>), so the game's bonus stays and this comes on top. Player overrides GetRunSpeedFactor
    /// without calling it, so players are never touched. Off, without a rider or at Ride 0 the factor is the game's.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.GetRunSpeedFactor))]
    public static class Gallop
    {
        /// <summary>How much faster a ridden mount runs at Ride 100, on top of the game's bonus.</summary>
        private const float FasterAt100 = 0.15f;

        /// <summary>The run speed multiplier at a rider's Ride factor (0..1).</summary>
        public static float Boost(float factor) => 1f + FasterAt100 * factor;

        [HarmonyPostfix]
        private static void Postfix(Character __instance, ref float __result)
        {
            if (!GameSkillSettings.Riding.Value || !__instance.HaveRider())
                return;
            __result *= HookGuard.Run("gallop", static mount => Boost(MountRider.Factor(mount)), __instance, 1f);
        }
    }
}
