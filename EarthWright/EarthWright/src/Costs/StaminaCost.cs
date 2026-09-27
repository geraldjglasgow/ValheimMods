using UnityEngine;

namespace EarthWright.Costs
{
    /// <summary>
    /// The stamina of one terrain swing: the game's own cost reshaped by "Stamina Mode" (or an entry's YAML stamina),
    /// then lowered by the chosen stamina skill; nothing during free build. Only the radius and the skill change it -
    /// a longer reach never lowers it ("no discount").
    /// </summary>
    internal static class StaminaCost
    {
        public static float For(CostContext ctx, float vanilla)
        {
            if (FreeBuild.On)
                return 0f;
            return Mathf.Max(0f, Base(ctx, vanilla) * SkillMultiplier(ctx.Player));
        }

        /// <summary>The game's own placement stamina for the held build tool; 0 without one.</summary>
        public static float Vanilla(Player player)
        {
            if (player == null || player.m_buildPieces == null || player.GetRightItem() == null)
                return 0f;
            return player.GetBuildStamina();
        }

        /// <summary>True while the game's own stamina check and cost stand unchanged for this use.</summary>
        public static bool IsVanilla(CostContext ctx)
        {
            return !FreeBuild.On && ctx.Override?.Stamina == null && CostSettings.Stamina.Value == StaminaMode.Vanilla
                && CostSettings.StaminaSkill.Value == Skills.SkillType.None;
        }

        private static float Base(CostContext ctx, float vanilla)
        {
            if (ctx.Override?.Stamina is float entryAmount)
                return entryAmount;
            switch (CostSettings.Stamina.Value)
            {
                case StaminaMode.Off:
                    return 0f;
                case StaminaMode.Fixed:
                    return CostSettings.StaminaPerUse.Value;
                case StaminaMode.Scaled:
                    return vanilla * CostSettings.StaminaFactor.Value * ctx.Scale(CostSettings.StaminaRadiusExponent.Value);
                default:
                    return vanilla;
            }
        }

        /// <summary>1 without a stamina skill; down to 1 - reduction at skill level 100.</summary>
        private static float SkillMultiplier(Player player)
        {
            Skills.SkillType skill = CostSettings.StaminaSkill.Value;
            if (skill == Skills.SkillType.None || player == null)
                return 1f;
            float share = Mathf.Clamp01(CostSettings.StaminaSkillReduction.Value / 100f);
            return 1f - share * Mathf.Clamp01(player.GetSkillFactor(skill));
        }
    }
}
