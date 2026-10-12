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
            page.Perk("Long Roll", 0f, LongRollTip(page));
            page.Perk("Counter", DodgeCounter.UnlockLevel,
                $"Dodging through an attack readies a counter for {SkillPage.Duration(DodgeCounter.WindowSeconds)}: the next melee attack you start deals {SkillPage.Percent(DodgeCounter.DamageBonus)} more damage with every hit it lands. Bows, staffs and throws neither use it up nor gain from it.");
            page.Perk("Heavy Roll", HeavyRoll.UnlockLevel, HeavyRollTip(page.Player));
        }

        private static string LongRollTip(SkillPage page)
        {
            float share = LongRoll.ExtraShare(page.Factor);
            string tip = $"Your roll keeps you untouchable {SkillPage.Percent(share)} longer at your level, {SkillPage.Percent(LongRoll.ExtraShare(1f))} at level 100.";
            float window = LongRoll.LastWindow;
            return window > 0f ? $"{tip} Your last roll: {window:0.00} s untouchable without it, {window * (1f + share):0.00} s with it." : tip;
        }

        private static string HeavyRollTip(Player player)
        {
            float cost = player.GetDodgeStaminaUse();
            return $"You can dodge while over-encumbered, at x{SkillPage.Number(HeavyRoll.Cost)} stamina: {SkillPage.Number(cost * HeavyRoll.Cost)} a roll now, against {SkillPage.Number(cost)} for an ordinary one.";
        }
    }
}
