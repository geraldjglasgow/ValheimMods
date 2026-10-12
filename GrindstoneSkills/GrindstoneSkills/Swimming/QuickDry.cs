using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Quick Dry, on the local player's own client, which owns the player and so runs its status effects. The game adds
    /// Wet (SEMan.s_statusEffectWet) every frame the player is in water (Character.UpdateWater) or out in the rain
    /// (Player.UpdateEnvStatusEffects), through SEMan.AddStatusEffect with resetTime: the first call clones the effect from
    /// ObjectDB and returns the clone; every later call only restarts its clock and returns null. So the effect counts
    /// down its m_ttl only once the player is out of the water or the rain, and shortening the clone's m_ttl as it is made
    /// shortens exactly the drying time.
    /// <list type="bullet">
    /// <item>The clone's m_ttl is multiplied by 1 - <see cref="ShorterAt100"/> x the Swim factor at that moment. The
    /// prefab in ObjectDB is never touched, so other players and creatures dry as in the game.</item>
    /// <item>Off, or at Swim 0, the clone keeps the game's time.</item>
    /// </list>
    /// </summary>
    public static class QuickDry
    {
        /// <summary>How much shorter Wet lasts at Swim 100.</summary>
        private const float ShorterAt100 = 0.5f;

        /// <summary>The share of Wet's time left at a Swim factor (0..1).</summary>
        public static float Kept(float factor) => 1f - ShorterAt100 * factor;

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), typeof(StatusEffect), typeof(bool), typeof(int), typeof(float), typeof(short))]
        private static class Add
        {
            [HarmonyPostfix]
            private static void Postfix(SEMan __instance, StatusEffect __result)
            {
                if (__result is null || __result.NameHash() != SEMan.s_statusEffectWet || !GameSkillSettings.Swimming.Value)
                    return;
                if (LocalSkill.IsLocal(__instance.m_character))
                    HookGuard.Run("quick dry", static wet => Shorten(wet), __result);
            }
        }

        private static void Shorten(StatusEffect wet)
        {
            float kept = Kept(LocalSkill.Factor(Skills.SkillType.Swim));
            if (wet.m_ttl > 0f && kept < 1f)
                wet.m_ttl *= kept;
        }
    }
}
