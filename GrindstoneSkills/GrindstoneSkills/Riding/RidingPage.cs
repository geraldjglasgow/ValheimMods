namespace GrindstoneSkills
{
    /// <summary>
    /// Ride's page in the info pane: what the Riding perks (section 41) add at the player's level, each always on
    /// (<see cref="Gallop"/>, <see cref="SteadySeat"/>), with the numbers they use. The mount's owner applies them from the
    /// level the player's client sends while riding, which is this level. Written on the local player's client as the page
    /// opens; nothing while the server has Riding off.
    /// </summary>
    public static class RidingPage
    {
        public static void Write(SkillPage page)
        {
            if (!GameSkillSettings.Riding.Value)
                return;
            float factor = page.Factor;
            page.Perk("Gallop", 0f,
                $"A mount you ride runs {SkillPage.Percent(Gallop.Boost(factor) - 1f)} faster, on top of the game's gallop speed above. +{SkillPage.Percent(Gallop.Boost(1f) - 1f)} at level 100.");
            page.Perk("Steady Seat", 0f,
                $"A mount takes {SkillPage.Percent(1f - SteadySeat.Kept(factor))} less damage while you ride it, drowning included. {SkillPage.Percent(1f - SteadySeat.Kept(1f))} at level 100.");
        }
    }
}
