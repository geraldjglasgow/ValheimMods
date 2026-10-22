using System;
using System.Collections.Generic;
using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The casters' status effects, made once and added to every item database the game wakes or copies (on every peer:
    /// a status effect travels by name hash, so the owner of a character a chant lands on must know it): one per chant
    /// (<see cref="Spell.Effect"/>, the game's own <c>SE_Stats</c>, <see cref="Spell.ChantSeconds"/> long) and its longer
    /// twin for a Mossweave wearer (<see cref="LongSuffix"/>), each robe set's bonus (<see cref="RobeSet.Bonus"/>: a
    /// resistance or Blood Magic, or only a mark that the set is complete, read by <see cref="SpellCost"/> and
    /// <see cref="SpellCast"/>), and the stamina a Sunweave wearer's heals give (<see cref="HealStaminaName"/>).
    /// </summary>
    public static class CasterStatus
    {
        public const string LongSuffix = "_Long";
        public const string HealStaminaName = "EE_SE_SunweaveStamina";
        private const float LongChant = 1.5f;
        private const float HealStamina = 10f;
        private const float Instant = 0.1f;

        private static bool built;
        private static readonly List<StatusEffect> made = new List<StatusEffect>();

        public static int HealStaminaHash => HealStaminaName.GetStableHashCode();

        public static void Prepare(List<StatusEffect> effects)
        {
            if (effects == null)
                return;
            Build();
            foreach (StatusEffect effect in made)
            {
                if (!effects.Contains(effect))
                    effects.Add(effect);
            }
        }

        private static void Build()
        {
            if (built)
                return;
            built = true;
            Chant(Spells.Hearthsong, se => se.m_healthRegenMultiplier = 1.5f);
            Chant(Spells.Swiftness, se =>
            {
                se.m_speedModifier = 0.1f;
                se.m_runStaminaDrainModifier = -0.2f;
            });
            foreach (RobeSet set in RobeSets.All)
                Bonus(set);
            SE_Stats stamina = Make(HealStaminaName, "", "", null, Instant);
            stamina.m_staminaUpFront = HealStamina;
            made.Add(stamina);
        }

        /// <summary>A chant's effect and its longer twin.</summary>
        private static void Chant(Spell spell, Action<SE_Stats> stats)
        {
            SE_Stats effect = Make(spell.Effect, spell.Token, "", spell.Icon, Spell.ChantSeconds);
            SE_Stats longer = Make(spell.Effect + LongSuffix, spell.Token, "", spell.Icon, Spell.ChantSeconds * LongChant);
            stats(effect);
            stats(longer);
            made.Add(effect);
            made.Add(longer);
        }

        private static void Bonus(RobeSet set)
        {
            SE_Stats effect = Make(set.BonusName, Lang.Token(set.SetWord), Lang.Token(set.SetWord + "_tip"), Spells.Mend.Icon, 0f);
            set.Bonus?.Invoke(effect);
            set.SetBonus = effect;
            made.Add(effect);
            RobeItems.Join(set);
        }

        private static SE_Stats Make(string name, string word, string tooltip, string icon, float seconds)
        {
            SE_Stats effect = ScriptableObject.CreateInstance<SE_Stats>();
            effect.name = name;
            effect.m_name = word;
            effect.m_tooltip = tooltip;
            effect.m_icon = icon != null ? CasterBundles.Sprite(CasterBundles.Spells, icon) : null;
            effect.m_ttl = seconds;
            return effect;
        }
    }
}
