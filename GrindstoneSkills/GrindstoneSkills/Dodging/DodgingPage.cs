using Translations;

namespace GrindstoneSkills
{
    /// <summary>
    /// Dodge's page in the info pane: the Dodging perks (section 40) after the game's own lines, each with its numbers
    /// at the player's level and at 100, from the same constants the perks use. Written on the local player's client
    /// as the page opens; with the switch off it adds nothing.
    /// </summary>
    public static class DodgingPage
    {
        public static void Write(SkillPage page)
        {
            if (!DodgeSkill.Active)
                return;
            page.Perk(Lang.Get("gs_perk_longroll"), 0f, LongRollTip(page));
            page.Perk(Lang.Get("gs_perk_counter"), DodgeCounter.UnlockLevel,
                Lang.Format("gs_dodge_counter_tip", SkillPage.Duration(DodgeCounter.WindowSeconds), SkillPage.Percent(DodgeCounter.DamageBonus)));
            page.Perk(Lang.Get("gs_perk_heavyroll"), HeavyRoll.UnlockLevel, HeavyRollTip(page.Player));
        }

        private static string LongRollTip(SkillPage page)
        {
            float share = LongRoll.ExtraShare(page.Factor);
            string tip = Lang.Format("gs_dodge_longroll_tip", SkillPage.Percent(share), SkillPage.Percent(LongRoll.ExtraShare(1f)));
            float window = LongRoll.LastWindow;
            if (window <= 0f)
                return tip;
            return tip + " " + Lang.Format("gs_dodge_longroll_last", window.ToString("0.00"), (window * (1f + share)).ToString("0.00"));
        }

        private static string HeavyRollTip(Player player)
        {
            float cost = player.GetDodgeStaminaUse();
            return Lang.Format("gs_dodge_heavyroll_tip", SkillPage.Number(HeavyRoll.Cost), SkillPage.Number(cost * HeavyRoll.Cost), SkillPage.Number(cost));
        }
    }
}
