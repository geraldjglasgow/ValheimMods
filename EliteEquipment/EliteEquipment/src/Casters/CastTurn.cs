using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The caster turns to the friend a heal is locked on to, whatever the camera looks at (user, 2026-10-09: "make the
    /// animation look natural and points the wand just toward the target no matter the players actual looking
    /// position"): a quick, smooth turn of the body (<see cref="DegreesPerSecond"/>, a half turn in about a quarter of a
    /// second, before the cast lands) that keeps facing a friend who moves until the cast is over (at most
    /// <see cref="Seconds"/>). The camera and the player's look are left alone; the wand's cast lets the game turn nothing
    /// meanwhile (<see cref="WandItem"/>: its rotation speed is 0), so the two never fight. Local player only, from the
    /// HUD's update.
    /// </summary>
    public static class CastTurn
    {
        private const float DegreesPerSecond = 720f;
        private const float Seconds = 1f;

        private static Character target;
        private static float until;
        private static bool attacking;

        public static void Begin(Character friend)
        {
            target = friend;
            until = Time.time + Seconds;
            attacking = false;
        }

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            if (!Turning(player))
            {
                target = null;
                return;
            }
            Vector3 toward = target.transform.position - player.transform.position;
            toward.y = 0f;
            if (toward.sqrMagnitude < 0.01f)
                return;
            Quaternion yaw = Quaternion.RotateTowards(player.transform.rotation, Quaternion.LookRotation(toward),
                DegreesPerSecond * Time.deltaTime);
            player.transform.rotation = yaw;
            if (player.m_body != null)
                player.m_body.rotation = yaw;
        }

        /// <summary>Still turning: a friend, a wand in hand, within the time, and the cast not yet over (once begun).</summary>
        private static bool Turning(Player player)
        {
            if (target == null || Time.time > until || player == null || CasterGearWorn.Wand(player) == null)
                return false;
            bool inAttack = player.InAttack();
            if (attacking && !inAttack)
                return false;
            attacking |= inAttack;
            return true;
        }
    }
}
