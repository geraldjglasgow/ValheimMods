using System.Collections.Generic;
using EliteCreaturesReborn.Visuals;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// A lingering pool of poison reconstructed from a Miasmic creature's replicated trail. Every machine spawns one to
    /// draw it; only the machine that owns the creature marks it <c>damaging</c> and lets it poison what stands in it,
    /// so two clients never double-hit and a client with effects off still takes exactly the owner's poison. It harms
    /// PLAYERS ONLY, with the game's own Poison: it applies a poison-typed hit whose strength is <c>cloud damage</c>, and
    /// RPC_Damage converts that into the vanilla SE_Poison status effect - the same debuff, icon and tick as a blob's.
    /// It carries no attacker, so it neither scales with mutations nor feeds Leeching. Strength, life and radius arrive
    /// at spawn.
    /// </summary>
    public sealed class PoisonCloud : MonoBehaviour
    {
        private float _lifetime;
        private float _damage;
        private float _radius;
        private bool _damaging;
        private float _age;
        private float _tick;
        private readonly List<Character> _found = new List<Character>();

        public static void Spawn(Vector3 position, float lifetime, float damage, float radius, string effect, bool damaging)
        {
            GameObject holder = new GameObject("ecr_miasma_cloud");
            holder.transform.position = position;
            PoisonCloud cloud = holder.AddComponent<PoisonCloud>();
            cloud._lifetime = lifetime;
            cloud._damage = damage;
            cloud._radius = radius;
            cloud._damaging = damaging;
            GameObject? prefab = EffectResolver.Resolve(effect, EffectResolver.Cloud, "Miasmic cloud effect");
            LingeringVisual.Attach(holder, prefab, lifetime, radius);
        }

        private void Update() => Guard.Run("PoisonCloud.Update", Step);

        private void Step()
        {
            _age += Time.deltaTime;
            if (_age >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }
            if (!_damaging) // remote clients draw the cloud but never decide its damage
            {
                return;
            }
            _tick -= Time.deltaTime;
            if (_tick <= 0f)
            {
                _tick = 1f;
                Poison();
            }
        }

        // Clouds hurt PLAYERS ONLY. Filtering to Player excludes all three cases the spec calls out as real bugs: other
        // creatures (a Miasmic pack would poison itself to death), tamed animals (killed by a fight the player never
        // chose), and the creature that made them (which would entangle Miasmic with Devouring's meals).
        private void Poison()
        {
            _found.Clear();
            Character.GetCharactersInRange(transform.position, _radius, _found);
            foreach (Character character in _found)
            {
                if (character is Player player && !player.IsDead())
                {
                    player.Damage(BuildHit());
                }
            }
        }

        private HitData BuildHit()
        {
            HitData hit = new HitData();
            hit.m_damage.m_poison = _damage;
            hit.m_point = transform.position;
            hit.m_hitType = HitData.HitType.Poisoned;
            return hit;
        }
    }
}
