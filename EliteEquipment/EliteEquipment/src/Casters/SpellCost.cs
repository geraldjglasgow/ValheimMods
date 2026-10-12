using HarmonyLib;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A wand's eitr. The game prices every attack of a weapon from its primary attack's eitr
    /// (<c>Attack.GetAttackEitr</c> reads <c>m_shared.m_attack.m_attackEitr</c>, read 2026-10-09), so the two attacks cannot
    /// cost differently on their own: the bolt (the projectile attack) costs <see cref="BoltEitr"/>, up to a third less from
    /// Elemental Magic (user, 2026-10-11: eitr, not stamina), the cast costs the spell being cast
    /// (<see cref="SpellCast.Casting"/>): its eitr, a quarter less for Mend with the whole Deerhide Vestments on, then up
    /// to a third less from Blood Magic, the game's own reduction for staves (0.33 times the skill factor).
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
    public static class SpellCost
    {
        private const float BoltEitr = 4f;
        private const float SetSaving = 0.25f;
        private const float SkillSaving = 0.33f;

        [HarmonyPostfix]
        private static void Postfix(Attack __instance, Character character, ItemDrop.ItemData weapon, ref float __result)
        {
            if (!CasterGearWorn.IsWand(weapon))
                return;
            if (character == null || character != Player.m_localPlayer)
            {
                __result = 0f;
                return;
            }
            if (__instance.m_attackType != Attack.AttackType.None)
            {
                __result = WindUp.Prepaid ? 0f : BoltEitr * (1f - SkillSaving * character.GetSkillFactor(Skills.SkillType.ElementalMagic));
                return;
            }
            Spell spell = SpellCast.Casting ?? Loadout.In(Player.m_localPlayer, weapon, CasterControls.Slot);
            __result = spell != null ? Of(character, spell) : 0f;
        }

        public static float Of(Character caster, Spell spell)
        {
            float eitr = spell.Eitr;
            if (spell == Spells.Mend && caster.GetSEMan().HaveStatusEffect(RobeSets.Meadows.BonusHash))
                eitr *= 1f - SetSaving;
            return eitr * (1f - SkillSaving * caster.GetSkillFactor(Skills.SkillType.BloodMagic));
        }
    }
}
