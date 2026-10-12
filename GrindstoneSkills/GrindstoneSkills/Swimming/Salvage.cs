using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Salvage, on the local player's own client: Player.AutoPickup runs there each physics frame (Player.FixedUpdate, the
    /// local player only) and pulls in loose items within m_autoPickupRange (2 m in the game), using that one field both
    /// for its sphere query and for its distance check. While the player swims (Character.IsSwimming), a prefix widens the
    /// field to 1 + <see cref="ReachAt100"/> x the Swim factor times its value for this one call, and a finalizer puts the
    /// value back on every path, so the pickup range is the game's (or another mod's) everywhere else. Picking up an item
    /// another peer owns goes through the game's own ownership request. Off, out of the water or at Swim 0 the range is
    /// not touched.
    /// The prefix runs last and the finalizer first, so this save and restore sits inside any other mod's save and
    /// restore of the same field (FeastMaster's, for one): Harmony runs finalizers in prefix order, not reversed.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.AutoPickup))]
    public static class Salvage
    {
        /// <summary>How much farther items are picked up while swimming at Swim 100: 1 is twice as far.</summary>
        private const float ReachAt100 = 1f;

        /// <summary>The pickup range's multiplier while swimming at a Swim factor (0..1).</summary>
        public static float Reach(float factor) => 1f + ReachAt100 * factor;

        [HarmonyPrefix, HarmonyPriority(Priority.Last)]
        private static void Prefix(Player __instance, out float __state)
        {
            __state = -1f;
            if (!ReferenceEquals(__instance, Player.m_localPlayer) || !GameSkillSettings.Swimming.Value || !__instance.IsSwimming())
                return;
            __state = HookGuard.Run("salvage", static player => Widen(player), __instance, -1f);
        }

        [HarmonyFinalizer, HarmonyPriority(Priority.First)]
        private static void Finalizer(Player __instance, float __state)
        {
            if (__state >= 0f)
                __instance.m_autoPickupRange = __state;
        }

        /// <summary>Widens the range and returns the value to restore, or -1 when it was left alone.</summary>
        private static float Widen(Player player)
        {
            float reach = Reach(LocalSkill.Factor(Skills.SkillType.Swim));
            float range = player.m_autoPickupRange;
            if (reach <= 1f || range <= 0f)
                return -1f;
            player.m_autoPickupRange = range * reach;
            return range;
        }
    }
}
