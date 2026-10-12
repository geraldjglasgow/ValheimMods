using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Elemental Magic's milestone, Attunement (section 43): at Elemental Magic level 50, +25% eitr regeneration while the
    /// player holds a weapon that trains Elemental Magic (Humanoid.GetCurrentWeapon: a staff). Runs on the local player's
    /// own client, which owns the player and regenerates their eitr. Read from the game code 2026-10-11: Player.UpdateStats
    /// (every fixed step) starts the eitr multiplier at 1, lets SEMan.ModifyEitrRegen raise it (a mead's bonus is added:
    /// SE_Stats adds its multiplier above 1), then adds the gear's eitr regeneration modifiers. The postfix adds 0.25 the
    /// same way, so it stacks with meads and gear as they stack with each other, and the game's regeneration delay after
    /// spending eitr is unchanged. Off, below level 50, or with anything else in hand, the multiplier is the game's.
    /// </summary>
    public static class Attunement
    {
        /// <summary>The eitr regeneration added to the multiplier while holding a staff.</summary>
        private const float EitrRegenBonus = 0.25f;

        /// <summary>The perk's tip on the Elemental Magic page.</summary>
        public static string Tip =>
            $"+{SkillPage.Percent(EitrRegenBonus)} eitr regeneration while you hold an Elemental Magic staff. Adds up with the regeneration bonuses of meads and gear.";

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyEitrRegen))]
        private static class Regen
        {
            [HarmonyPostfix]
            private static void Postfix(SEMan __instance, ref float eitrMultiplier)
            {
                if (__instance.m_character is Player player && ReferenceEquals(player, Player.m_localPlayer))
                    eitrMultiplier += HookGuard.Run("attunement", static p => Bonus(p), player, 0f);
            }
        }

        private static float Bonus(Player player) =>
            RangedMastery.Trains(player.GetCurrentWeapon(), Skills.SkillType.ElementalMagic)
            && RangedMastery.Active(Skills.SkillType.ElementalMagic) ? EitrRegenBonus : 0f;
    }
}
