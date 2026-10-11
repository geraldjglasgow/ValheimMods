using System;
using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// The game's damage numbers and resistances by the damage types a definition names (<see cref="DamageKind"/>):
    /// `character: resistances:` from <c>Character.m_damageModifiers</c>, and an attack's `damage:` from its item's
    /// <c>m_damages</c>. The two types the files have no word for (untyped damage, and the damage only creatures take)
    /// are noted when they are not the plain value.
    /// </summary>
    internal static class DamageExport
    {
        private const float MaxDamage = 1000000f;

        private static readonly DamageKind[] Kinds = (DamageKind[])Enum.GetValues(typeof(DamageKind));

        /// <summary>`resistances:`, every damage type with the game's modifier word.</summary>
        public static void WriteResistances(ExportWriter writer, HitData.DamageModifiers modifiers)
        {
            writer.Open("resistances");
            foreach (DamageKind kind in Kinds)
            {
                HitData.DamageModifier modifier = Modifier(modifiers, kind);
                string key = ExportValues.Word(kind);
                if (Enum.IsDefined(typeof(HitData.DamageModifier), modifier))
                {
                    writer.Key(key, ExportValues.Word(modifier));
                }
                else
                {
                    writer.Note($"{key}: {modifier} - not one of the game's modifiers, so it is left as it is");
                }
            }
            writer.Close();
            if (modifiers.m_nonPlayer != HitData.DamageModifier.Normal)
            {
                writer.Note($"against damage meant for creatures only it is {ExportValues.Word(modifiers.m_nonPlayer)}, "
                    + "which a definition cannot set");
            }
        }

        /// <summary>
        /// An attack's `damage:`, the types it deals; a note when it deals none of its own. With <paramref name="every"/>
        /// (a custom creature's copy, read back as a fresh copy of its original) every type is written, 0 included, since
        /// a type the definitions set to 0 would otherwise come back as the original's.
        /// </summary>
        public static void WriteDamage(ExportWriter writer, HitData.DamageTypes damages, bool every)
        {
            List<DamageKind> dealt = new List<DamageKind>(Array.FindAll(Kinds, kind => every || Amount(damages, kind) != 0f));
            if (dealt.Count == 0)
            {
                writer.Note("damage: none of its own - it hurts by what it fires or spawns, or not at all");
            }
            else
            {
                writer.Open("damage");
                dealt.ForEach(kind => writer.Number(ExportValues.Word(kind), Amount(damages, kind), 0f, MaxDamage));
                writer.Close();
            }
            if (damages.m_damage != 0f || damages.m_nonPlayer != 0f)
            {
                writer.Note($"it also deals {ExportValues.Number(damages.m_damage)} untyped damage and "
                    + $"{ExportValues.Number(damages.m_nonPlayer)} damage to creatures only, which a definition cannot set");
            }
        }

        public static HitData.DamageModifier Modifier(HitData.DamageModifiers modifiers, DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Blunt: return modifiers.m_blunt;
                case DamageKind.Slash: return modifiers.m_slash;
                case DamageKind.Pierce: return modifiers.m_pierce;
                case DamageKind.Chop: return modifiers.m_chop;
                case DamageKind.Pickaxe: return modifiers.m_pickaxe;
                case DamageKind.Fire: return modifiers.m_fire;
                case DamageKind.Frost: return modifiers.m_frost;
                case DamageKind.Lightning: return modifiers.m_lightning;
                case DamageKind.Poison: return modifiers.m_poison;
                default: return modifiers.m_spirit;
            }
        }

        public static float Amount(HitData.DamageTypes damages, DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Blunt: return damages.m_blunt;
                case DamageKind.Slash: return damages.m_slash;
                case DamageKind.Pierce: return damages.m_pierce;
                case DamageKind.Chop: return damages.m_chop;
                case DamageKind.Pickaxe: return damages.m_pickaxe;
                case DamageKind.Fire: return damages.m_fire;
                case DamageKind.Frost: return damages.m_frost;
                case DamageKind.Lightning: return damages.m_lightning;
                case DamageKind.Poison: return damages.m_poison;
                default: return damages.m_spirit;
            }
        }
    }
}
