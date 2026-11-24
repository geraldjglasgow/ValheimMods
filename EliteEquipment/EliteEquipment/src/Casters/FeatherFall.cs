using System.Collections.Generic;
using LocalEffects;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Feather Fall (features/casters.md, "Nine more spells"): a chant cast like Hearthsong (<see cref="SpellCast"/>:
    /// everyone friendly within <see cref="Spell.ChantRange"/>, the caster too) for <see cref="Seconds"/> (half again for a
    /// Mossweave wearer, as every chant), whose status effect is the feather cape's: the game's <c>SE_Stats</c> with the
    /// cape's (<c>SlowFall</c>) highest fall speed and fall damage modifier, copied from the item database as it is prepared
    /// (<see cref="GameBorrow"/>; until then the numbers read from the game on 2026-11-03), and its icon while ours is
    /// missing. Its release (<c>ee_fx_cast_featherfall</c>, one shot) swirls round every friend within reach on each machine
    /// that draws (<see cref="Show"/>).
    /// </summary>
    public static class FeatherFall
    {
        public const float Seconds = 30f;
        private const float FallSpeed = 5f;
        private const float FallDamage = -1f;

        private static readonly List<SE_Stats> made = new List<SE_Stats>();
        private static readonly List<Character> near = new List<Character>();

        public static void Stats(SE_Stats effect)
        {
            effect.m_maxMaxFallSpeed = FallSpeed;
            effect.m_fallDamageModifier = FallDamage;
            effect.m_ttl *= Seconds / Spell.ChantSeconds;
            made.Add(effect);
        }

        /// <summary>The feather cape's own numbers, and its icon while ours is missing, into both our effects.</summary>
        public static void Borrow(SE_Stats cape)
        {
            foreach (SE_Stats effect in made)
            {
                effect.m_maxMaxFallSpeed = cape.m_maxMaxFallSpeed;
                effect.m_fallDamageModifier = cape.m_fallDamageModifier;
                if (effect.m_icon == null)
                    effect.m_icon = cape.m_icon;
            }
        }

        /// <summary>The feathers round every friend of the caster within reach here: null when drawn, else the shared ring to draw.</summary>
        public static string Show(Vector3 at, Character caster)
        {
            GameObject prefab = CasterFx.Prepared(SpellFx.CastFeatherfall);
            if (prefab == null)
                return CasterFx.Chant;
            Player player = caster as Player;
            near.Clear();
            Character.GetCharactersInRange(at, Spell.ChantRange, near);
            foreach (Character character in near)
            {
                if (player != null ? FriendTarget.Friendly(character, player) : character.IsPlayer())
                    LocalEffect.Attach(prefab, character.transform, character.transform.position, endless: false);
            }
            near.Clear();
            ReleaseDraws.Sound(SpellFx.CastFeatherfall, at);
            return null;
        }
    }
}
