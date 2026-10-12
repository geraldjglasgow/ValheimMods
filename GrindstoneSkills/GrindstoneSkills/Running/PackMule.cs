using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Pack Mule, Running's perk from level 0: the local player carries up to <see cref="CarryAt100"/> more at Run 100,
    /// linear in the level. A postfix on Player.GetMaxCarryWeight adds it after the game, which has already applied gear
    /// and status effects (SEMan.ModifyMaxCarryWeight) and the world's carry weight rate; the bonus is scaled by that
    /// same rate, as the base and a belt are.
    /// <para>Runs on the player's own client, the only machine that decides whether they are encumbered (Player.UpdateStats,
    /// Jump, Dodge, auto pickup) and that shows their weight. Every other player returns at the first test, so another
    /// client and a dedicated server always see the game's value. Off, or at Run 0, the game's value is returned
    /// unchanged.</para>
    /// </summary>
    public static class PackMule
    {
        private const float CarryAt100 = 50f;

        /// <summary>The extra carry weight at a Run factor (0..1), with the world's carry weight rate.</summary>
        public static float Bonus(float factor) => CarryAt100 * factor * Game.m_carryWeightRate;

        [HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
        private static class MaxWeight
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, ref float __result)
            {
                if (!LocalSkill.IsLocal(__instance) || !GameSkillSettings.Running.Value)
                    return;
                __result += HookGuard.Run("pack mule", static skill => Bonus(LocalSkill.Factor(skill)), Skills.SkillType.Run, 0f);
            }
        }
    }
}
