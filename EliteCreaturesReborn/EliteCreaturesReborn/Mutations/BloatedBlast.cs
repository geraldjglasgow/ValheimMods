using System.Collections.Generic;
using EliteCreaturesReborn.Visuals;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The Bloated explosion itself, fired the instant the owner's fuse ends (the delay is spent by the corpse rider, see
    /// <see cref="BloatedCorpse"/>). It is driven from a routed broadcast (see <see cref="Runtime.EliteRpc"/>): every
    /// client draws the burst at the owner's corpse position, but only the owner's copy is <c>damaging</c> and deals the
    /// blunt damage in a radius, scaled by star count as (1 + stars). It has no attacker, so it feeds nothing.
    /// </summary>
    public static class BloatedBlast
    {
        public static void Detonate(Vector3 pos, float baseDamage, float radius, int stars, string blastEffect, bool damaging)
        {
            GameObject? prefab = EffectResolver.Resolve(blastEffect, EffectResolver.Blast, "Bloated blast effect");
            CosmeticClone.Flash(prefab, pos, radius); // every machine draws the blast
            if (!damaging) // only the owner's blast decides damage; remote copies are visual only
            {
                return;
            }
            List<Character> found = new List<Character>();
            Character.GetCharactersInRange(pos, radius, found);
            float damage = baseDamage * (1 + stars);
            foreach (Character character in found)
            {
                if (character != null && !character.IsDead())
                {
                    character.Damage(BuildHit(pos, damage));
                }
            }
        }

        private static HitData BuildHit(Vector3 pos, float damage)
        {
            HitData hit = new HitData();
            hit.m_damage.m_blunt = damage;
            hit.m_point = pos;
            hit.m_pushForce = 40f;
            hit.m_hitType = HitData.HitType.EnemyHit;
            return hit;
        }
    }
}
