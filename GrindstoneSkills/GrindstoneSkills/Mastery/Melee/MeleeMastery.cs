namespace GrindstoneSkills
{
    /// <summary>
    /// The melee weapon milestones that change the local player's hits, through <see cref="OutgoingHits"/>: one for each
    /// of the seven melee skills, opened at <see cref="Level"/> of that skill and picked by the skill the hit trains
    /// (HitData.m_skill, the weapon's skill: Attack.DoMeleeAttack, DoAreaAttack and the projectile a thrown spear
    /// becomes all copy it from the weapon).
    /// <list type="bullet">
    /// <item>Runs on the attacker's client, where the game builds the hit and sends it through Character.Damage to the
    /// target's owner. Each milestone changes only fields the game sends with the hit (damage, poison, stagger
    /// multiplier, push force), so the target's owner applies them by its own rules in Character.RPC_Damage, on any
    /// peer, a dedicated server included.</item>
    /// <item>The level is the attacker's own (skills live only on their client), read only for these seven skills: the
    /// game's Skills.GetSkill adds an entry for any skill it is asked about.</item>
    /// <item>Weapon Mastery off, or the skill below 50, leaves the hit exactly as the game built it.</item>
    /// </list>
    /// </summary>
    public static class MeleeMastery
    {
        /// <summary>The level of its own skill at which each melee milestone opens.</summary>
        public const float Level = 50f;

        public static void OnHit(Character target, HitData hit)
        {
            if (GameSkillSettings.WeaponMastery.Value && HasMilestone(hit.m_skill) && LocalSkill.Reached(hit.m_skill, Level))
                Apply(target, hit);
        }

        private static bool HasMilestone(Skills.SkillType skill) => skill switch
        {
            Skills.SkillType.Swords or Skills.SkillType.Knives or Skills.SkillType.Clubs or Skills.SkillType.Polearms
                or Skills.SkillType.Spears or Skills.SkillType.Axes or Skills.SkillType.Unarmed => true,
            _ => false,
        };

        private static void Apply(Character target, HitData hit)
        {
            switch (hit.m_skill)
            {
                case Skills.SkillType.Swords: SwordsEdge.OnHit(target, hit); break;
                case Skills.SkillType.Knives: KnivesBleed.OnHit(hit); break;
                case Skills.SkillType.Clubs: ClubsCrush.OnHit(hit); break;
                case Skills.SkillType.Polearms: PolearmsSweep.OnHit(hit); break;
                case Skills.SkillType.Spears: SpearsLongThrow.OnHit(hit); break;
                case Skills.SkillType.Axes: AxesRend.OnHit(target, hit); break;
                case Skills.SkillType.Unarmed: UnarmedBrawler.OnHit(hit); break;
            }
        }
    }
}
