using System.Collections.Generic;
using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Where Eir's Spring, Chain Mend, Second Wind and Revive (features/casters.md, "Nine more spells") join the shared
    /// casting code: <see cref="SpellCast"/> asks <see cref="Ready"/> as a cast starts, takes the marked friend only for a
    /// spell that is <see cref="Marked"/>, and hands the landing to <see cref="Cast"/>; <see cref="CasterStatus"/> takes
    /// their status effects from <see cref="Effects"/>.
    /// </summary>
    public static class HealSpells
    {
        public static bool Casts(Spell spell) =>
            spell == Spells.Spring || spell == Spells.ChainMend || spell == Spells.SecondWind || spell == Spells.Revive;

        /// <summary>
        /// Not aimed at the marked friend (the spring at the ground, the wind all round, Revive at a grave): no lock and no
        /// turn to them.
        /// </summary>
        public static bool Unmarked(Spell spell) => spell == Spells.Spring || spell == Spells.SecondWind || spell == Spells.Revive;

        public static void Cast(Player caster, ItemDrop.ItemData wand, Spell spell, Character locked, Vector3 tip)
        {
            if (spell == Spells.Spring)
                Spring.Cast(caster, wand, spell);
            else if (spell == Spells.ChainMend)
                ChainMend.Cast(caster, wand, spell, locked, tip);
            else if (spell == Spells.SecondWind)
                SecondWind.Cast(caster, spell);
            else if (spell == Spells.Revive)
                Revive.Cast(caster, spell);
        }

        /// <summary>False, with the message why, when the spell cannot start now (Revive without a grave or while it waits).</summary>
        public static bool Ready(Player caster, Spell spell, out string why)
        {
            string word = spell == Spells.Revive ? Revive.Refusal(caster) : null;
            why = word != null ? Lang.Get(word) : null;
            return word == null;
        }

        public static void Effects(List<StatusEffect> made)
        {
            made.Add(SecondWind.Make());
            made.Add(ReviveCooldown.Make());
        }
    }
}
