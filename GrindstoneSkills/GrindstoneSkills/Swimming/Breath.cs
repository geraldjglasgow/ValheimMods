using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Breath, on the local player's own client, ticked by <see cref="LocalPlayerTick"/>. While the player dives
    /// (<see cref="Diving.Under"/>) with their eyes under the water, breath runs out over <see cref="Seconds"/>: 20 s
    /// untrained, 60 s at Swim 100. With none left the player drowns: a twentieth of max health each second, the game's own drowning hit
    /// (HitType.Drowning, through Character.Damage, so Long Breath softens it as well). Above the water breath comes back
    /// in <see cref="RefillSeconds"/>. A Breath icon with the seconds left shows among the status effects while breath is
    /// short (<see cref="BreathIcon"/>). A swimmer at the surface never loses breath, however rough the sea. Not diving
    /// and with full breath the tick costs a switch read and a time check. Off, nobody runs out of breath.
    /// </summary>
    public static class Breath
    {
        private const float UntrainedSeconds = 20f;
        private const float MoreAt100 = 40f;

        /// <summary>Seconds from empty to full breath above the water.</summary>
        private const float RefillSeconds = 3f;

        /// <summary>A drowning hit, as a share of max health, once a second: the game's own.</summary>
        public const float DrownShare = 0.05f;

        private static float left = -1f;
        private static float drownTimer;

        /// <summary>Seconds of breath at a Swim factor (0..1).</summary>
        public static float Seconds(float factor) => UntrainedSeconds + MoreAt100 * factor;

        public static void Tick(Player player, float dt)
        {
            bool under = GameSkillSettings.Swimming.Value && Diving.Under && !player.IsDead()
                && player.m_waterLevel > player.m_eye.position.y;
            if (under || left >= 0f)
                HookGuard.Run("breath", static tick => Step(tick.player, tick.dt, tick.under), (player, dt, under));
        }

        private static void Step(Player player, float dt, bool under)
        {
            float full = Seconds(LocalSkill.Factor(Skills.SkillType.Swim));
            if (left < 0f)
                left = full;
            if (under)
                Hold(player, dt);
            else
                left += full / RefillSeconds * dt;
            if (!under && left >= full)
            {
                left = -1f;
                BreathIcon.Hide(player);
                return;
            }
            BreathIcon.Show(player, left);
        }

        private static void Hold(Player player, float dt)
        {
            left = Mathf.Max(0f, left - dt);
            if (left > 0f)
            {
                drownTimer = 0f;
                return;
            }
            drownTimer += dt;
            if (drownTimer < 1f)
                return;
            drownTimer = 0f;
            Drown(player);
        }

        private static void Drown(Player player)
        {
            HitData hit = new HitData();
            hit.m_damage.m_damage = Mathf.Ceil(player.GetMaxHealth() * DrownShare);
            hit.m_point = player.GetCenterPoint();
            hit.m_dir = Vector3.down;
            hit.m_hitType = HitData.HitType.Drowning;
            player.Damage(hit);
            player.m_drownEffects.Create(player.GetCenterPoint(), player.transform.rotation);
        }
    }
}
