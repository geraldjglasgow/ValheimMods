using System.Collections.Generic;
using LocalEffects;
using StatusTemplates;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Second Wind (features/casters.md, "Nine more spells"): every player within <see cref="Spell.ChantRange"/>, the caster
    /// too, gets <see cref="Stamina"/> back at once. Stamina lives on each player's own machine, so it travels the game's
    /// own way, as the Sunweave heal's stamina does: the caster adds <see cref="EffectName"/> by name hash
    /// (<c>SEMan.AddStatusEffect</c>, which the game sends to the player's owner) and that one-moment <c>SE_Stats</c>
    /// gives its <c>m_staminaUpFront</c> in its <c>Setup</c> there (<c>Character.AddStamina</c>), then ends. Tames get
    /// none (no stamina to fill). The ring (<c>ee_fx_cast_secondwind</c>, rolling out to 10 m as authored, its +Z the
    /// caster's facing) goes to every player near.
    /// </summary>
    public static class SecondWind
    {
        public const string EffectName = "EE_SE_SecondWind";
        public const float Stamina = 50f;

        private const float Moment = 0.1f;
        // The shared chant ring, authored to the chant's 10 m, drawn as authored when the bundle lacks the wind's own.
        private const float AsAuthored = 4f;
        private const string Sound = "sfx_Potion_stamina_Start";

        private static readonly List<Character> near = new List<Character>();

        public static void Cast(Player caster, Spell spell)
        {
            int effect = EffectName.GetStableHashCode();
            near.Clear();
            Character.GetCharactersInRange(caster.transform.position, Spell.ChantRange, near);
            foreach (Character character in near)
            {
                if (character.IsPlayer() && FriendTarget.Friendly(character, caster))
                    character.GetSEMan().AddStatusEffect(effect, resetTime: true);
            }
            near.Clear();
            CasterFx.Send(HealReleases.SecondWind, caster.transform.position, caster.GetZDOID(), spell);
        }

        /// <summary>The carrier, in every item database with the casters' other effects (<see cref="CasterStatus"/>).</summary>
        public static StatusEffect Make()
        {
            SE_Stats effect = StatusTemplate.Make<SE_Stats>(EffectName);
            effect.m_name = "";
            effect.m_tooltip = "";
            effect.m_icon = null;
            effect.m_ttl = Moment;
            effect.m_staminaUpFront = Stamina;
            return effect;
        }

        /// <summary>
        /// The ring on this machine at the caster's feet, turned to the caster's facing and rolling out to the 10 m
        /// (<see cref="SpellFx.Ground"/>, as the chants' rings); the gold chant ring without it.
        /// </summary>
        public static void Show(Vector3 at, Character caster)
        {
            GameObject prefab = CasterFx.Prepared(HealReleases.SecondWind);
            if (prefab != null)
                SpellFx.Ground(prefab, at, caster);
            else
                LocalEffect.Flash(CasterFx.Prepared(CasterFx.Chant), at, AsAuthored);
            LocalEffect.Sound(ZNetScene.instance?.GetPrefab(Sound), at);
        }
    }
}
