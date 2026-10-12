using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// How much faster one of the local player's skills trains right now: Catch Up (section 6, every skill) and Weapon
    /// Switch (section 43, the weapon skills). Read from the game code 2026-10-11: every experience gain ends in
    /// Skills.RaiseSkill on the gaining player's own client, the game's skills and the mod's own alike (Player.RaiseSkill
    /// applies the status effects first, Rested among them; the dodge and a tamed creature's master call it directly).
    /// It hands the factor to Skills.Skill.Raise, which multiplies it by the skill's step and the world's skill-gain
    /// rate. One prefix there multiplies the factor, as the other modules' experience prefixes on the same method do,
    /// so they all multiply with each other in any order.
    /// <list type="bullet">
    /// <item>Catch Up: a skill whose base level is below its best (<see cref="BestLevels"/>) gains experience
    /// <c>CatchUpRate</c> times as fast, 2, until it is back there.</item>
    /// <item>Weapon Switch: a weapon skill below the player's highest weapon skill (<see cref="WeaponSwitch"/>) gains
    /// <c>WeaponSwitchRate</c> times as fast, 1.5, until it reaches it.</item>
    /// </list>
    /// When both apply only the larger counts. With both switches off the factor is never touched: the game's pace.
    /// Skills live on the player's own client, so other players' gains are never seen here.
    /// </summary>
    public static class CatchUpXp
    {
        public enum Boost
        {
            None,
            CatchUp,
            WeaponSwitch,
        }

        private const float CatchUpRate = 2f;
        private const float WeaponSwitchRate = 1.5f;

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class Raise
        {
            [HarmonyPrefix]
            private static void Prefix(Skills __instance, Skills.SkillType skillType, ref float factor)
            {
                if (!DeathSettings.CatchUp.Value && !GameSkillSettings.WeaponMastery.Value)
                    return;
                if (LocalSkill.IsLocal(__instance.m_player))
                    factor *= HookGuard.Run("catch up experience", static raise => Rate(raise.skills, raise.skillType), (skills: __instance, skillType), 1f);
            }
        }

        /// <summary>
        /// What speeds one of the local player's skills now: the boost, how many times as fast it trains (1 for none)
        /// and the base level it lasts until. Catch Up is asked first because its rate is the larger; only one applies.
        /// </summary>
        public static Boost Find(Skills skills, Skills.SkillType type, out float rate, out int until)
        {
            int level = BestLevels.BaseLevel(skills, type);
            if (CatchingUp(skills, type, level, out until))
            {
                rate = CatchUpRate;
                return Boost.CatchUp;
            }
            if (Switching(skills, type, level, out until))
            {
                rate = WeaponSwitchRate;
                return Boost.WeaponSwitch;
            }
            rate = 1f;
            until = 0;
            return Boost.None;
        }

        private static float Rate(Skills skills, Skills.SkillType type)
        {
            Find(skills, type, out float rate, out _);
            return rate;
        }

        private static bool CatchingUp(Skills skills, Skills.SkillType type, int level, out int best)
        {
            best = DeathSettings.CatchUp.Value ? BestLevels.Of(skills.m_player, type) : 0;
            return level < best;
        }

        private static bool Switching(Skills skills, Skills.SkillType type, int level, out int highest)
        {
            bool weapon = GameSkillSettings.WeaponMastery.Value && WeaponSwitch.IsWeapon(type);
            highest = weapon ? WeaponSwitch.Highest(skills) : 0;
            return level < highest;
        }
    }
}
