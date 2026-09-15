using HarmonyLib;

namespace FeastMaster
{
    /// <summary>
    /// Base values: Base Health and Base Stamina are written into the player's fields right before the game sums
    /// base and food values (so the HUD base bar and the totals follow, hot reloaded on the next food update), and
    /// the stamina from skills is added to the stamina total afterwards.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    public static class BaseValuesPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance)
        {
            __instance.m_baseHP = Settings.BaseHealth.Value;
            __instance.m_baseStamina = Settings.BaseStamina.Value;
        }

        [HarmonyPostfix]
        public static void Postfix(Player __instance, ref float stamina)
        {
            stamina += SkillBonus(__instance);
        }

        /// <summary>The total stamina the player's skills add to the base, per the section 5 settings.</summary>
        public static float SkillBonus(Player player)
        {
            return Bonus(player, Skills.SkillType.Run, Settings.RunSkillStamina.Value)
                + Bonus(player, Skills.SkillType.Jump, Settings.JumpSkillStamina.Value)
                + Bonus(player, Skills.SkillType.Sneak, Settings.SneakSkillStamina.Value)
                + Bonus(player, Skills.SkillType.Swim, Settings.SwimSkillStamina.Value)
                + Bonus(player, Skills.SkillType.Fishing, Settings.FishingSkillStamina.Value);
        }

        private static float Bonus(Player player, Skills.SkillType skill, float atSkill100)
        {
            return atSkill100 == 0f ? 0f : atSkill100 * player.GetSkillFactor(skill);
        }
    }
}
