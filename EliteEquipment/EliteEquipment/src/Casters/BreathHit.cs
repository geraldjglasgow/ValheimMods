using UnityEngine;

using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One burst's hit on one target for Moder's Breath, built as Elder's Volley builds a seed's (<see cref="VolleyHit"/>,
    /// the game's bolt): the held wand's damage with its quality turned to frost and spirit (its spirit stays spirit, the
    /// rest, whatever its types, becomes frost: Frostsilver's is already both), times <see cref="Share"/>, the bolt attack's
    /// damage multiplier and the caster's random Elemental Magic factor, then the caster's status effects
    /// (<c>SEMan.ModifyAttack</c>). The frost slows by the game's own rule: the target's owner adds the game's Frost status
    /// (<c>Character.AddFrostDamage</c>, <c>SE_Frost</c>) for any frost damage taken. A breath does not push.
    /// </summary>
    public static class BreathHit
    {
        /// <summary>Each burst's share of the bolt's damage (features/casters.md, "Nine more spells": x 0.5).</summary>
        public const float Share = 0.5f;

        public static HitData For(Player caster, ItemDrop.ItemData wand, Vector3 point, Vector3 way)
        {
            SharedData shared = wand.m_shared;
            HitData hit = VolleyHit.Base(caster, wand);
            float factor = caster.GetRandomSkillFactor(shared.m_skillType);
            hit.m_damage = Frosted(wand.GetDamage());
            hit.m_damage.Modify(Share * shared.m_attack.m_damageMultiplier * factor);
            hit.m_pushForce = 0f;
            hit.m_point = point;
            hit.m_dir = way;
            caster.GetSEMan().ModifyAttack(shared.m_skillType, ref hit);
            return hit;
        }

        /// <summary>The wand's damage as frost and spirit: spirit kept, every other type's amount as frost.</summary>
        private static HitData.DamageTypes Frosted(HitData.DamageTypes wand) => new HitData.DamageTypes
        {
            m_frost = wand.m_blunt + wand.m_slash + wand.m_pierce + wand.m_fire + wand.m_frost + wand.m_lightning
                + wand.m_poison,
            m_spirit = wand.m_spirit,
        };
    }
}
