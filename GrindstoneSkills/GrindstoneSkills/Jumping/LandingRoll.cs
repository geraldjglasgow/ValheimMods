using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Roll, Jumping's milestone at <see cref="UnlockLevel"/>: landing from a fall that would hurt while the move input
    /// points forward (within 60 degrees of where the player looks; autorun counts) plays the game's dodge roll in that
    /// direction. A rolled fall up to <see cref="FreeHeights"/> times the Soft Landing safe height does no damage; a
    /// longer one does <see cref="RolledDamageShare"/> of what Soft Landing left. <see cref="Landing"/> asks for it on
    /// the landing tick.
    /// <para>The roll is the game's dodge without its costs or its protection: the body turns to the direction, the
    /// "dodge" animation trigger is set through ZSyncAnimation (an RPC to every peer, so everyone sees it, and the root
    /// motion moves the player), with the dodge's noise and effects, as Player.UpdateDodge does. It spends no stamina and
    /// gives no Dodge experience: it is forced by the landing, not chosen, and costing stamina would leave a tired player
    /// with full damage. It grants no invulnerability: m_dodgeInvincible is cleared before the trigger, so the
    /// dodge-invincible flag the game publishes (ZDOVars.s_dodgeinv) stays false and hits land as usual, with no perfect
    /// dodge, Counter or adrenaline. It rolls only when the game would let the player dodge: alive, not attacking,
    /// dodging, staggering, knocked back or over-encumbered.</para>
    /// <para>Runs on the player's own client, which owns the player, computes the fall and applies the damage.</para>
    /// </summary>
    public static class LandingRoll
    {
        private const float UnlockLevel = 50f;
        private const float FreeHeights = 2f;
        private const float RolledDamageShare = 0.5f;
        /// <summary>The smallest dot product of the move input and the flat look direction that counts as forward.</summary>
        private const float ForwardInput = 0.5f;
        /// <summary>The game's own threshold for move input (Character.CheckRun, Player.UpdateStats).</summary>
        private const float MinMoveInput = 0.1f;
        /// <summary>The noise of the game's dodge (Player.UpdateDodge).</summary>
        private const float RollNoise = 5f;

        /// <summary>The Jump level that unlocks Roll.</summary>
        public static float Level => UnlockLevel;

        /// <summary>The share of Soft Landing's damage a rolled fall above the free height does.</summary>
        public static float Share => RolledDamageShare;

        /// <summary>The highest rolled fall that does no damage at a Jump factor (0..1), in metres.</summary>
        public static float FreeHeight(float factor) => SoftLanding.SafeHeight(factor) * FreeHeights;

        /// <summary>A rolled fall's damage, from the damage Soft Landing left.</summary>
        public static float Damage(float softDamage, float height, float factor) =>
            height <= FreeHeight(factor) ? 0f : softDamage * RolledDamageShare;

        /// <summary>Plays the roll when the local player has the level, can dodge and moves forward; true when it rolled.</summary>
        public static bool TryRoll(Player player)
        {
            if (!LocalSkill.Reached(Skills.SkillType.Jump, UnlockLevel) || !CanRoll(player) || !Forward(player, out Vector3 direction))
                return false;
            Play(player, direction);
            return true;
        }

        private static bool CanRoll(Player player) =>
            !player.IsDead() && !player.InAttack() && !player.InDodge() && !player.IsStaggering() && !player.IsKnockedBack() && !player.IsEncumbered();

        /// <summary>The flat move direction, when the move input points forward.</summary>
        private static bool Forward(Player player, out Vector3 direction)
        {
            direction = player.m_moveDir;
            direction.y = 0f;
            Vector3 look = player.m_lookDir;
            look.y = 0f;
            if (direction.magnitude < MinMoveInput || look.sqrMagnitude < 0.0001f)
                return false;
            direction.Normalize();
            return Vector3.Dot(direction, look.normalized) >= ForwardInput;
        }

        private static void Play(Player player, Vector3 direction)
        {
            player.ClearActionQueue();
            player.m_dodgeInvincible = false;
            Quaternion facing = Quaternion.LookRotation(direction);
            player.transform.rotation = facing;
            player.m_body.rotation = facing;
            player.m_zanim.SetTrigger("dodge");
            player.AddNoise(RollNoise);
            player.m_dodgeEffects.Create(player.transform.position, Quaternion.identity, player.transform, 1f, -1, player.GetZDOID());
        }
    }
}
