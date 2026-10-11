using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// `character:` on the shell's <see cref="Character"/> (a Humanoid for a human): the name players see
    /// (<c>m_name</c>; the game localizes it everywhere it shows it, which leaves plain text as typed and translates a
    /// <c>$word</c>), health at one star (<c>m_health</c>), speeds (<see cref="Speeds"/>), faction, the boss flag and the
    /// boss fight (<see cref="BossFight"/>), resistances (<c>m_damageModifiers</c>, one field per damage type), what hurts
    /// it (<c>m_tolerateWater</c>, <c>m_tolerateFire</c>, <c>m_tolerateSmoke</c>: "hurt by" is the opposite of the game's
    /// "tolerates") and <c>m_staggerWhenBlocked</c>.
    /// </summary>
    internal static class CharacterFields
    {
        public static void Apply(CreatureBuild build, Character character)
        {
            CharacterBlock? block = build.Definition.Character;
            if (block == null)
            {
                return;
            }
            ApplyIdentity(block, character);
            Speeds.Apply(block, character);
            ApplyHurts(block, character);
            ApplyResistances(block.Resistances, character);
            if (block.BossFight != null)
            {
                BossFight.Apply(build, character, block.BossFight);
            }
        }

        private static void ApplyIdentity(CharacterBlock block, Character character)
        {
            if (block.DisplayName != null)
            {
                character.m_name = block.DisplayName;
            }
            if (block.Faction != null)
            {
                character.m_faction = block.Faction.Value;
            }
            Assign.Set(ref character.m_health, block.Health);
            Assign.Set(ref character.m_boss, block.Boss);
        }

        private static void ApplyHurts(CharacterBlock block, Character character)
        {
            Assign.Set(ref character.m_tolerateWater, Not(block.HurtByWater));
            Assign.Set(ref character.m_tolerateFire, Not(block.HurtByFire));
            Assign.Set(ref character.m_tolerateSmoke, Not(block.HurtBySmoke));
            Assign.Set(ref character.m_staggerWhenBlocked, block.StaggersWhenBlocked);
        }

        private static bool? Not(bool? value) => value == null ? (bool?)null : !value.Value;

        /// <summary>The types the definition names; the others keep the base's. The modifiers are a struct: copied, changed, put back.</summary>
        private static void ApplyResistances(Dictionary<DamageKind, HitData.DamageModifier>? resistances, Character character)
        {
            if (resistances == null)
            {
                return;
            }
            HitData.DamageModifiers modifiers = character.m_damageModifiers;
            foreach (KeyValuePair<DamageKind, HitData.DamageModifier> resistance in resistances)
            {
                Slot(ref modifiers, resistance.Key) = resistance.Value;
            }
            character.m_damageModifiers = modifiers;
        }

        private static ref HitData.DamageModifier Slot(ref HitData.DamageModifiers modifiers, DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Blunt: return ref modifiers.m_blunt;
                case DamageKind.Slash: return ref modifiers.m_slash;
                case DamageKind.Pierce: return ref modifiers.m_pierce;
                case DamageKind.Chop: return ref modifiers.m_chop;
                case DamageKind.Pickaxe: return ref modifiers.m_pickaxe;
                case DamageKind.Fire: return ref modifiers.m_fire;
                case DamageKind.Frost: return ref modifiers.m_frost;
                case DamageKind.Lightning: return ref modifiers.m_lightning;
                case DamageKind.Poison: return ref modifiers.m_poison;
                default: return ref modifiers.m_spirit;
            }
        }
    }
}
