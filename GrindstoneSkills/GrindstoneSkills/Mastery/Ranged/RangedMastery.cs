namespace GrindstoneSkills
{
    /// <summary>
    /// The ranged and magic milestones of Weapon Mastery (section 43), each at level 50 of its own skill: Bows Recover
    /// (<see cref="BowRecover"/>), Crossbows Steady Reload (<see cref="SteadyReload"/>), Elemental Magic Attunement
    /// (<see cref="Attunement"/>) and Blood Magic Life Tap (<see cref="LifeTap"/>), all on the local player's own client,
    /// which owns the player, their attacks and their inventory; plus the Blood Magic experience a cast shield gives its
    /// caster when it absorbs a hit (<see cref="ShieldXp"/>), which starts on the shielded character's owner. None of them
    /// changes a hit, so <see cref="OnHit"/>, which <see cref="OutgoingHits"/> calls for every hit the local player deals to
    /// another character, does nothing. With the Weapon Mastery switch off, or below level 50, the game plays as its own.
    /// </summary>
    public static class RangedMastery
    {
        /// <summary>The level of its own skill at which each milestone opens.</summary>
        private const float MilestoneLevel = 50f;

        /// <summary>The level each milestone opens at, for the skill pages.</summary>
        public static float Level => MilestoneLevel;

        /// <summary>Whether Weapon Mastery is on and the local player has reached the milestone level in this skill.</summary>
        public static bool Active(Skills.SkillType skill) =>
            GameSkillSettings.WeaponMastery.Value && LocalSkill.Reached(skill, MilestoneLevel);

        /// <summary>Whether the weapon trains this skill.</summary>
        public static bool Trains(ItemDrop.ItemData weapon, Skills.SkillType skill) =>
            weapon != null && weapon.m_shared.m_skillType == skill;

        public static void OnHit(Character target, HitData hit)
        {
        }
    }
}
