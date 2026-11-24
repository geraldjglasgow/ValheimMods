namespace EliteEquipment.Casters
{
    /// <summary>
    /// War Chant (features/casters.md, "Nine more spells"): a chant cast like Hearthsong (<see cref="SpellCast"/>: everyone
    /// friendly within <see cref="Spell.ChantRange"/>, the caster too, for <see cref="Spell.ChantSeconds"/>, longer for a
    /// Mossweave wearer, <see cref="CasterStatus"/>) whose status effect is the game's own <c>SE_Stats</c> attack modifier:
    /// the hits of every skill (<c>m_modifyAttackSkill</c> All, read by the game's <c>SE_Stats.ModifyAttack</c> on the
    /// attacker's machine) deal <see cref="Damage"/> times their damage. Its ring (<c>ee_fx_cast_warchant</c>) rolls out to
    /// the chant's reach as Hearthsong's does (<see cref="ReleaseDraws"/>).
    /// </summary>
    public static class WarChant
    {
        public const float Damage = 1.1f;

        public static void Stats(SE_Stats effect)
        {
            effect.m_modifyAttackSkill = Skills.SkillType.All;
            effect.m_damageModifier = Damage;
        }
    }
}
