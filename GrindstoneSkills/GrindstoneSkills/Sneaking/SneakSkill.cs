namespace GrindstoneSkills
{
    /// <summary>
    /// The Sneaking module's switch (section 38, <see cref="GameSkillSettings.Sneaking"/>, synced, so every machine agrees)
    /// and the local player's Sneak factor, shared by its perks: <see cref="Prowl"/>, <see cref="SoftSteps"/>,
    /// <see cref="SneakAttack"/> and <see cref="Vanish"/>. The factor is read on the local player's own client, where that
    /// player's levels live; it includes food and mead bonuses, as the game's own Sneak effects do.
    /// </summary>
    public static class SneakSkill
    {
        public const Skills.SkillType Type = Skills.SkillType.Sneak;

        /// <summary>Whether the Sneaking perks are on.</summary>
        public static bool On => GameSkillSettings.Sneaking.Value;

        /// <summary>The local player's Sneak level as a share of 100, 0..1.</summary>
        public static float Factor => LocalSkill.Factor(Type);
    }
}
