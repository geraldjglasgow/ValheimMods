using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Whether a creature the server runs could need its bones now, so it must animate in full. What the game reads of a
    /// creature's bones on its owner:
    /// <list type="bullet">
    /// <item>An attack's origin joint (a jaw, a hand): melee sweeps, area attacks, projectile spawn points, start and
    /// trigger effects, what a boss spawns (Attack.GetAttackOrigin), only inside an attack.</item>
    /// <item>A mount's attach point: the server sets its copy of the rider there (ZSyncTransform), and its creatures hit
    /// and sense that copy.</item>
    /// <item>Effects on a named child (equip, hit, death), state and animation-event effects at a joint, footsteps at a
    /// foot (from the footstep curve on the owner only, within the camera's footstep range, so never on a dedicated
    /// server; a few clips' footstep events), and the head's look target: looks only. The ragdoll spawns at the root in
    /// its own pose; it copies no bones.</item>
    /// </list>
    /// Sight and hearing use the root and EyePos, a child of the root, not a bone; feeding, taming, breeding and fleeing use
    /// the root; root motion is added only in attacks, dodges and emotes. So a creature needs its bones when it is a boss,
    /// dying, alerted, hurt in the last <see cref="HurtSeconds"/>, has a target (a creature, a building, or what it flees),
    /// is in an attack (the animator's attack tag), is ridden or is aggravated; or when a player is within
    /// <see cref="NearMeters"/> (for anything a mod hangs on its bones that touches players, and the effects players see),
    /// or, on a hosted game, the host's camera is within that or the creature's footstep range (the owner plays footsteps
    /// there from the animator's footstep curve). Cheapest tests first; no allocation.
    /// </summary>
    internal static class AnimRule
    {
        /// <summary>Seconds after damage a creature keeps animating in full (it turns, staggers, fights or flees).</summary>
        public const float HurtSeconds = 10f;

        /// <summary>A creature with a player this close animates in full.</summary>
        public const float NearMeters = 20f;

        public static bool NeedsBones(CreatureAnim anim)
        {
            Character body = anim.Body;
            BaseAI ai = anim.Ai;
            if (body == null || anim.Boss || body.IsDead())
            {
                return true;
            }
            if (ai.IsAlerted() || ai.m_timeSinceHurt < HurtSeconds || HasTarget(ai))
            {
                return true;
            }
            if (body.InAttack() || Ridden(anim.Saddle) || ai.IsAggravated())
            {
                return true;
            }
            Vector3 at = body.transform.position;
            return AnimPlayers.Near(at, NearMeters) || AnimPlayers.CameraNear(at, Mathf.Max(NearMeters, anim.StepRange));
        }

        // The game's own AIs keep their targets in fields; another mod's AI is asked the general way.
        private static bool HasTarget(BaseAI ai)
        {
            if (ai is MonsterAI monster)
            {
                return monster.m_targetCreature != null || monster.m_targetStatic != null;
            }
            if (ai is AnimalAI animal)
            {
                return animal.m_target != null;
            }
            return ai.GetTargetCreature() != null || ai.HaveTarget();
        }

        // A player in the saddle (the game's own test, made every physics step): the rider sits on a bone.
        private static bool Ridden(Sadle saddle) => saddle != null && saddle.isActiveAndEnabled && saddle.HaveValidUser();
    }
}
