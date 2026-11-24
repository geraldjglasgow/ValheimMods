using SharedData = ItemDrop.ItemData.SharedData;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One seed's hit for Elder's Volley, built as the game builds the bolt's (<c>Attack.FireProjectileBurst</c>, read
    /// 2026-11-02): the held wand's damage with its quality (<c>ItemData.GetDamage</c>), so the seed's types are the wand's
    /// (pierce, the Greywood's and Rootbound's blunt or the Frostsilver's frost, and spirit), times <see cref="Share"/>, the bolt attack's
    /// damage multiplier and the caster's random Elemental Magic factor, then the caster's status effects
    /// (<c>SEMan.ModifyAttack</c>). The skill and its raise are the bolt's, so each seed that hits a character raises
    /// Elemental Magic as a bolt does (the game's projectile raises it). No charge: a spell is never charged.
    /// </summary>
    public static class VolleyHit
    {
        /// <summary>Each seed's share of the bolt's damage (features/casters.md, spell 7: "about 60% of the wand's bolt").</summary>
        public const float Share = 0.6f;

        public static HitData For(Player caster, ItemDrop.ItemData wand)
        {
            SharedData shared = wand.m_shared;
            HitData hit = Base(caster, wand);
            float factor = caster.GetRandomSkillFactor(shared.m_skillType);
            hit.m_damage.Add(wand.GetDamage());
            hit.m_damage.Modify(Share * shared.m_attack.m_damageMultiplier * factor);
            hit.m_pushForce *= factor;
            caster.GetSEMan().ModifyAttack(shared.m_skillType, ref hit);
            return hit;
        }

        /// <summary>Everything of the bolt's hit but its damage.</summary>
        public static HitData Base(Player caster, ItemDrop.ItemData wand)
        {
            SharedData shared = wand.m_shared;
            HitData hit = new HitData
            {
                m_toolTier = (short)shared.m_toolTier,
                m_pushForce = shared.m_attackForce * shared.m_attack.m_forceMultiplier,
                m_backstabBonus = shared.m_backstabBonus,
                m_staggerMultiplier = shared.m_attack.m_staggerMultiplier,
                m_skill = shared.m_skillType,
                m_skillLevel = caster.GetSkillLevel(shared.m_skillType),
                m_skillRaiseAmount = shared.m_attack.m_raiseSkillAmount,
                m_itemLevel = (short)wand.m_quality,
                m_itemWorldLevel = (byte)wand.m_worldLevel,
                m_blockable = shared.m_blockable,
                m_dodgeable = shared.m_dodgeable,
                m_hitType = HitData.HitType.PlayerHit,
                m_variant = shared.m_hitVariant,
            };
            hit.SetAttacker(caster);
            return hit;
        }
    }
}
