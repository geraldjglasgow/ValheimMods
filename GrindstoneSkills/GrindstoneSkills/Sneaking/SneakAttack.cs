using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Assassin, on the attacker's own client, through <see cref="OutgoingHits"/> (every hit the local player deals to
    /// someone else, before it leaves for the target's owner).
    /// <list type="bullet">
    /// <item>A hit that carries a sneak attack bonus (HitData.m_backstabBonus above 1, set by Attack and Projectile from
    /// the weapon or projectile) on a creature (it has a BaseAI) gets that bonus multiplied by up to 1 +
    /// <see cref="BonusAt100"/> at Sneak 100, linear from level 0. The bonus travels with the hit, and the target's owner
    /// applies it by the game's own rules in Character.RPC_Damage: only when the creature is not alerted, at most once
    /// every 300 s per creature, and under the Passive Mobs world modifier only when it cannot see the attacker. So the
    /// perk makes a sneak attack bigger; it never makes one happen.</item>
    /// <item>A sneak attack also gives <see cref="Experience"/> Sneak experience. The owner decides whether the bonus
    /// applied and tells nobody, so the attacker's client judges it from what it can know: the creature's alerted state
    /// (BaseAI.IsAlerted, which every machine copies from the creature's ZDO), the game's 300 s timer
    /// (Character.m_backstabTime, kept only on the creature's owner, so it tells something only when this client owns
    /// the creature), a 300 s timer of its own per creature (by ZDOID) for this player, and the Passive Mobs sight test
    /// (BaseAI.CanSeeTarget, which runs on any machine). Approximations: an alert that has not reached this client yet,
    /// another player's sneak attack on the same creature, or a change of the creature's owner can give experience the
    /// owner did not back with a sneak attack, or withhold it once. Only a creature hostile to the player counts
    /// (BaseAI.IsEnemy), so tamed animals are no experience farm.</item>
    /// </list>
    /// Off: the bonus and the experience are the game's.
    /// </summary>
    public static class SneakAttack
    {
        private const float BonusAt100 = 0.5f;
        private const float Experience = 5f;

        /// <summary>The game's time between two sneak attacks on one creature (Character.RPC_Damage).</summary>
        private const float ResetTime = 300f;

        /// <summary>Time.time this player was last given experience for a sneak attack on each creature; pruned on use.</summary>
        private static readonly Dictionary<ZDOID, float> Credited = new Dictionary<ZDOID, float>();
        private static readonly List<ZDOID> Expired = new List<ZDOID>();

        /// <summary>The extra sneak attack damage at a Sneak factor, as a share of the game's sneak attack.</summary>
        public static float Bonus(float factor) => BonusAt100 * factor;

        /// <summary>Sneak experience for each sneak attack.</summary>
        public static float ExperiencePerAttack => Experience;

        /// <summary>Seconds the game waits between two sneak attacks on one creature.</summary>
        public static float ResetSeconds => ResetTime;

        public static void OnHit(Character target, HitData hit)
        {
            if (!SneakSkill.On || hit.m_backstabBonus <= 1f)
                return;
            BaseAI ai = target.GetBaseAI();
            Player player = Player.m_localPlayer;
            if (ai == null || player == null)
                return;
            if (LooksLikeSneakAttack(target, ai, player))
                Credit(player, target.GetZDOID());
            hit.m_backstabBonus *= 1f + Bonus(SneakSkill.Factor);
        }

        /// <summary>Whether the target's owner will most likely apply the sneak attack, judged before the hit leaves.</summary>
        private static bool LooksLikeSneakAttack(Character target, BaseAI ai, Player player)
        {
            if (target.IsDead() || ai.IsAlerted() || !BaseAI.IsEnemy(player, target))
                return false;
            float now = Time.time;
            ZDOID creature = target.GetZDOID();
            if (creature.IsNone() || now - target.m_backstabTime <= ResetTime || CreditedWithin(creature, now))
                return false;
            return !PassiveMobsSee(ai, player);
        }

        private static bool CreditedWithin(ZDOID creature, float now) =>
            Credited.TryGetValue(creature, out float at) && now - at <= ResetTime;

        /// <summary>Under the Passive Mobs world modifier the game allows a sneak attack only on a creature that cannot see the attacker.</summary>
        private static bool PassiveMobsSee(BaseAI ai, Player player) =>
            ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs) && ai.CanSeeTarget(player);

        private static void Credit(Player player, ZDOID creature)
        {
            float now = Time.time;
            Prune(now);
            Credited[creature] = now;
            player.RaiseSkill(SneakSkill.Type, Experience);
        }

        /// <summary>Forgets creatures whose 300 s are over, so the table holds only the last few sneak attacks.</summary>
        private static void Prune(float now)
        {
            Expired.Clear();
            foreach (KeyValuePair<ZDOID, float> entry in Credited)
            {
                if (now - entry.Value > ResetTime)
                    Expired.Add(entry.Key);
            }
            foreach (ZDOID creature in Expired)
                Credited.Remove(creature);
        }
    }
}
