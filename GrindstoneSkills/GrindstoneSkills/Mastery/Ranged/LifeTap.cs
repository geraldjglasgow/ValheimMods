using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Blood Magic's milestone, Life Tap (section 43): at Blood Magic level 50, blood magic spells that cost health cost
    /// 25% less health, after the game's own cut (33% at level 100). Runs on the caster's own client, which owns their
    /// character and runs their attacks. Read from the game code 2026-10-11: Attack.GetAttackHealth works out a spell's
    /// health cost (its flat cost plus its share of current health, less the weapon skill's cut), and every use of that cost
    /// asks it: the check as the attack starts (Attack.Start), the payment (Attack.Update) and the payment per burst
    /// (Attack.FireProjectileBurst). The postfix lowers the result for the local player's own attacks with a weapon that trains
    /// Blood Magic, so the check and the payment agree. Off, below level 50 or with any other weapon, the cost is the game's.
    /// </summary>
    public static class LifeTap
    {
        /// <summary>The share taken off a blood magic spell's health cost.</summary>
        private const float HealthCut = 0.25f;

        /// <summary>The perk's tip on the Blood Magic page.</summary>
        public static string Tip =>
            $"Blood magic spells that cost health cost {SkillPage.Percent(HealthCut)} less health, on top of the skill's own cut.";

        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackHealth))]
        private static class Cost
        {
            [HarmonyPostfix]
            private static void Postfix(Attack __instance, ref float __result)
            {
                if (__result > 0f && ReferenceEquals(__instance.m_character, Player.m_localPlayer))
                    __result *= HookGuard.Run("life tap", static attack => Share(attack), __instance, 1f);
            }
        }

        /// <summary>The share of the cost the caster pays.</summary>
        private static float Share(Attack attack) =>
            RangedMastery.Trains(attack.m_weapon, Skills.SkillType.BloodMagic) && RangedMastery.Active(Skills.SkillType.BloodMagic)
                ? 1f - HealthCut
                : 1f;
    }
}
