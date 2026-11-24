namespace EliteEquipment.Casters
{
    /// <summary>
    /// A wand's heal power: its own (more each quality), the held spellbook's own (<see cref="GearPiece.HealPower"/>), then up to half again from the
    /// caster's Blood Magic, the game's skill factor (0 to 1) times <see cref="SkillBonus"/>.
    /// </summary>
    public static class HealPower
    {
        public const float SkillBonus = 0.5f;

        public static float Of(Humanoid caster, ItemDrop.ItemData wand)
        {
            if (wand == null)
                return 0f;
            float power = Base(wand) + (CasterGearWorn.HeldBook(caster)?.HealPower ?? 0f);
            float skill = caster != null ? caster.GetSkillFactor(Skills.SkillType.BloodMagic) : 0f;
            return power * (1f + SkillBonus * skill);
        }

        /// <summary>The wand's own, by quality, without book or skill (the tooltip's number).</summary>
        public static float Base(ItemDrop.ItemData wand) => Base(wand, wand.m_quality);

        /// <summary>The wand's own at a quality.</summary>
        public static float Base(ItemDrop.ItemData wand, int quality) =>
            (CasterGearWorn.WandPiece(wand)?.HealPower ?? 0f) + CasterGear.HealPowerPerQuality * (quality - 1);
    }
}
