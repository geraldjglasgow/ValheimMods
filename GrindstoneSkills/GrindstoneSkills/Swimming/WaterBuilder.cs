using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Water Builder, the Swim level <see cref="Level"/> milestone, on the local player's own client. The game puts
    /// everything in the hands away while the player swims (Humanoid.UpdateEquipment hides them each frame) and refuses
    /// to equip anything there (Humanoid.EquipItem), so nobody can build from the water. For the local player with the
    /// level, a build tool (any item with build pieces: the hammer, the hoe, the cultivator, another mod's) is let
    /// through both: for the one call, a prefix sets the player's swim timer to "long out of the water", so the game's
    /// IsSwimming test inside says no, and a finalizer puts the timer back on every path. Placing then works as on land,
    /// at the surface or on the bottom while diving (<see cref="Diving"/>), with the game's own placement rules (pieces
    /// that may not stand in water still may not). Weapons, shields and torches are still put away. Off, below the level,
    /// or for anyone else, the game's rule stands.
    /// </summary>
    public static class WaterBuilder
    {
        public const float Level = 50f;

        /// <summary>A swim timer the game reads as not swimming (it counts swimming while under 0.5 s).</summary>
        private const float DryTimer = 999f;

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipment))]
        private static class KeepInHand
        {
            [HarmonyPrefix]
            private static void Prefix(Humanoid __instance, out float __state) => __state = Dry(__instance, __instance.m_rightItem);

            [HarmonyFinalizer]
            private static void Finalizer(Humanoid __instance, float __state) => Restore(__instance, __state);
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        private static class Equip
        {
            [HarmonyPrefix]
            private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, out float __state) => __state = Dry(__instance, item);

            [HarmonyFinalizer]
            private static void Finalizer(Humanoid __instance, float __state) => Restore(__instance, __state);
        }

        /// <summary>Makes the game see the local player as dry for a build tool; returns the timer to restore, or -1.</summary>
        private static float Dry(Humanoid humanoid, ItemDrop.ItemData item)
        {
            if (!ReferenceEquals(humanoid, Player.m_localPlayer) || item?.m_shared.m_buildPieces == null)
                return -1f;
            if (!GameSkillSettings.Swimming.Value || !humanoid.IsSwimming() || !LocalSkill.Reached(Skills.SkillType.Swim, Level))
                return -1f;
            float timer = humanoid.m_swimTimer;
            humanoid.m_swimTimer = DryTimer;
            return timer;
        }

        private static void Restore(Humanoid humanoid, float timer)
        {
            if (timer >= 0f)
                humanoid.m_swimTimer = timer;
        }
    }
}
