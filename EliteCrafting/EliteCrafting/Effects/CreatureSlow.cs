using StatusTemplates;
using UnityEngine;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// Hamstring: a hit from the local player carries this status effect's hash (only when the hit carries none of its
    /// own) and the slow percentage in the hit's skill-level field, which the game uses for nothing but the level of the
    /// carried status effect. The target's owner adds it through the game's own path (RPC_Damage →
    /// SEMan.AddStatusEffect(hash, ..., skillLevel) → <see cref="SetLevel"/>), so it works whoever owns the target; a
    /// repeat hit resets the timer and takes the new strength. Bosses and players are never slowed (checked by the
    /// attacker). Movement: the native speed field. Attacks: the owner plays attack animations slower while it lasts
    /// (the game resets animation speed when an attack ends).
    /// </summary>
    public sealed class EcfSlow : SE_Stats
    {
        public const string EffectName = "ECF_Hamstring";
        public const float Seconds = 2f;

        /// <summary>Judgement call: whatever the summed rolls, a hamstrung creature keeps at least 10% of its speed.</summary>
        private const float MaxSlow = 0.9f;

        public static readonly int Hash = EffectName.GetStableHashCode();

        private float _slow;

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            base.SetLevel(itemLevel, skillLevel);
            _slow = Mathf.Clamp(skillLevel / 100f, 0f, MaxSlow);
            m_speedModifier = -_slow;
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);
            if (_slow > 0f && m_character.InAttack())
            {
                m_character.m_zanim.SetSpeed(1f - _slow);
            }
        }
    }

    /// <summary>Attacker side of Hamstring, and <see cref="EcfSlow"/>'s template (in every ObjectDB: <see cref="EffectsFeature.Register"/>).</summary>
    internal static class CreatureSlow
    {
        public static readonly StatusTemplate<EcfSlow> Template = new StatusTemplate<EcfSlow>(EcfSlow.EffectName,
            se => (se.m_name, se.m_ttl) = ("$ecf_fx_hamstring", EcfSlow.Seconds));

        /// <summary>From the local player's outgoing hit (<see cref="OutgoingHits"/>).</summary>
        public static void Carry(AggregateValues v, Character target, HitData hit)
        {
            float slow = v[EffectKind.OnHitSlow];
            if (slow > 0f && hit.m_statusEffectHash == 0 && !target.IsBoss() && !target.IsPlayer())
            {
                hit.m_statusEffectHash = EcfSlow.Hash;
                hit.m_skillLevel = slow * 100f;
            }
        }
    }
}
