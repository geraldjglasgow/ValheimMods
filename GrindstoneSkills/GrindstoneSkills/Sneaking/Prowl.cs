using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Prowl, on the local player's own client: crouched walking is up to <see cref="SpeedAt100"/> faster at Sneak 100,
    /// linear from level 0.
    /// <list type="bullet">
    /// <item>Character.UpdateWalking takes m_crouchSpeed as the base speed while the character crouches (for a player,
    /// while the crouch animation plays: Player.IsCrouching) and then applies water, attacks and status effects to it.
    /// The prefix raises the local player's m_crouchSpeed for that one call and the finalizer puts it back, so gear,
    /// status effects and another mod's change to the field all still apply, multiplied.</item>
    /// <item>Not while over-encumbered: the game walks an encumbered player at the crouch speed whether they crouch or
    /// not, and Prowl does not make that faster.</item>
    /// <item>A player's movement runs on their own client, which owns their character; every other peer sees the faster
    /// walk through the game's position sync.</item>
    /// </list>
    /// Every other character's walk returns at the first test. Off, or at Sneak 0, the speed is the game's.
    /// The prefix runs last and the finalizer first, so this save and restore sits inside any other mod's save and
    /// restore of the same field (FeastMaster's, for one): Harmony runs finalizers in prefix order, not reversed.
    /// </summary>
    public static class Prowl
    {
        private const float SpeedAt100 = 0.5f;

        /// <summary>No change made this update: nothing to put back.</summary>
        private const float Unchanged = -1f;

        /// <summary>The crouched speed bonus at a Sneak factor, as a share.</summary>
        public static float Bonus(float factor) => SpeedAt100 * factor;

        [HarmonyPatch(typeof(Character), nameof(Character.UpdateWalking))]
        private static class Walk
        {
            [HarmonyPrefix, HarmonyPriority(Priority.Last)]
            private static void Prefix(Character __instance, out float __state)
            {
                __state = Unchanged;
                if (ReferenceEquals(__instance, Player.m_localPlayer) && SneakSkill.On)
                    __state = HookGuard.Run("prowl", static player => Raise(player), __instance, Unchanged);
            }

            [HarmonyFinalizer, HarmonyPriority(Priority.First)]
            private static void Finalizer(Character __instance, float __state)
            {
                if (__state >= 0f)
                    __instance.m_crouchSpeed = __state;
            }
        }

        /// <summary>Raises the crouch speed for this update; returns the speed to put back, or <see cref="Unchanged"/>.</summary>
        private static float Raise(Character player)
        {
            if (!player.IsCrouching())
                return Unchanged;
            float bonus = Bonus(SneakSkill.Factor);
            if (bonus <= 0f || player.IsEncumbered())
                return Unchanged;
            float speed = player.m_crouchSpeed;
            player.m_crouchSpeed = speed * (1f + bonus);
            return speed;
        }
    }
}
