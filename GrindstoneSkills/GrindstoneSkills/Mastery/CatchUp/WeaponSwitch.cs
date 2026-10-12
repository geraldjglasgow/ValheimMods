namespace GrindstoneSkills
{
    /// <summary>
    /// Weapon Switch's measure (section 43): which skills are weapon skills (the eleven the game trains by hitting with
    /// a weapon, Blocking, Pickaxes and Woodcutting left out) and the player's highest base level among them, the stored
    /// level without food or mead bonuses. Read from the local player's own Skills, on their own client, where the game
    /// keeps them; eleven dictionary lookups per weapon skill gain, nothing kept.
    /// </summary>
    public static class WeaponSwitch
    {
        private static readonly Skills.SkillType[] weapons =
        {
            Skills.SkillType.Swords, Skills.SkillType.Knives, Skills.SkillType.Clubs, Skills.SkillType.Polearms,
            Skills.SkillType.Spears, Skills.SkillType.Axes, Skills.SkillType.Bows, Skills.SkillType.Crossbows,
            Skills.SkillType.Unarmed, Skills.SkillType.ElementalMagic, Skills.SkillType.BloodMagic,
        };

        public static bool IsWeapon(Skills.SkillType type) =>
            type is Skills.SkillType.Swords or Skills.SkillType.Knives or Skills.SkillType.Clubs
                or Skills.SkillType.Polearms or Skills.SkillType.Spears or Skills.SkillType.Axes or Skills.SkillType.Bows
                or Skills.SkillType.Crossbows or Skills.SkillType.Unarmed or Skills.SkillType.ElementalMagic
                or Skills.SkillType.BloodMagic;

        /// <summary>The highest base level among the player's weapon skills, whole; 0 when none is trained.</summary>
        public static int Highest(Skills skills)
        {
            int highest = 0;
            foreach (Skills.SkillType weapon in weapons)
            {
                int level = BestLevels.BaseLevel(skills, weapon);
                if (level > highest)
                    highest = level;
            }
            return highest;
        }
    }
}
