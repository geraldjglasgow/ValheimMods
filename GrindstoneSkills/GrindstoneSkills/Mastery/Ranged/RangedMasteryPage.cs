namespace GrindstoneSkills
{
    /// <summary>
    /// The bow, crossbow and magic skills' pages: each skill's Weapon Mastery milestone (section 43) as a perk at level 50,
    /// and on the Blood Magic page the shield experience as a perk from the start. Nothing with the switch off. Each tip is
    /// written beside the numbers it names, in the milestone's own class.
    /// </summary>
    public static class RangedMasteryPage
    {
        public static void Write(SkillPage page)
        {
            if (!GameSkillSettings.WeaponMastery.Value)
                return;
            switch (page.Type)
            {
                case Skills.SkillType.Bows:
                    page.Perk("Recover", RangedMastery.Level, BowRecover.Tip);
                    break;
                case Skills.SkillType.Crossbows:
                    page.Perk("Steady Reload", RangedMastery.Level, SteadyReload.Tip);
                    break;
                case Skills.SkillType.ElementalMagic:
                    page.Perk("Attunement", RangedMastery.Level, Attunement.Tip);
                    break;
                case Skills.SkillType.BloodMagic:
                    page.Perk("Life Tap", RangedMastery.Level, LifeTap.Tip);
                    page.Perk("Shield Training", 0f, ShieldXp.Tip);
                    break;
            }
        }
    }
}
