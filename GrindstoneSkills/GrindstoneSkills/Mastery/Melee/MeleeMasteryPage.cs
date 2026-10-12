namespace GrindstoneSkills
{
    /// <summary>The melee weapon skills' pages: each skill's Weapon Mastery milestone (section 43). Weapon Switch's line is <see cref="CatchUpPage"/>'s.</summary>
    public static class MeleeMasteryPage
    {
        public static void Write(SkillPage page)
        {
            if (GameSkillSettings.WeaponMastery.Value)
                Milestone(page);
        }

        /// <summary>The page's own skill's milestone, at <see cref="MeleeMastery.Level"/>; nothing for any other skill.</summary>
        private static void Milestone(SkillPage page)
        {
            switch (page.Type)
            {
                case Skills.SkillType.Swords: SwordsEdge.Perk(page); break;
                case Skills.SkillType.Knives: KnivesBleed.Perk(page); break;
                case Skills.SkillType.Clubs: ClubsCrush.Perk(page); break;
                case Skills.SkillType.Polearms: PolearmsSweep.Perk(page); break;
                case Skills.SkillType.Spears: SpearsLongThrow.Perk(page); break;
                case Skills.SkillType.Axes: AxesRend.Perk(page); break;
                case Skills.SkillType.Unarmed: UnarmedBrawler.Perk(page); break;
            }
        }
    }
}
