using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Vanish, the level 75 milestone, ticked on the local player's own client by <see cref="LocalPlayerTick"/>. Once
    /// the player has crouched for <see cref="QuietTime"/> seconds with no hostile creature within
    /// <see cref="ClearRadius"/> m while something hunts them, every hostile creature within <see cref="Reach"/> m that
    /// hunts them loses track of them; then not again for <see cref="Cooldown"/> seconds. "Vanished" shows over the player.
    /// <list type="bullet">
    /// <item>Crouched is the crouch animation playing (Player.IsCrouching), the game's own test for stealth
    /// (Player.UpdateStealth): standing up, or attacking, which plays the attack instead, starts the time over.</item>
    /// <item>Hunted is the game's own signal: a creature whose target is this player tells the player's client so, on
    /// whichever machine simulates it (Player.OnTargeted, IsTargeted: within the last second).</item>
    /// <item>Creatures are simulated by their owner, so the losing of track happens there: <see cref="VanishScan"/> finds
    /// the creatures near enough and sends each one's owner <see cref="Keys.RpcVanish"/>, and
    /// <see cref="VanishRpc"/> on that owner lets go of the target only if it is this player.</item>
    /// <item>The cooldown starts only when at least one creature was told, so crouching quietly with nobody hunting
    /// spends nothing.</item>
    /// </list>
    /// The nearby creatures are looked at every <see cref="ScanInterval"/> seconds while the player crouches, has the
    /// level and is off cooldown; otherwise the tick costs a switch read and the crouch test. Off, or below level 75,
    /// creatures hunt as in the game.
    /// </summary>
    public static class Vanish
    {
        private const float Level = 75f;
        private const float QuietTime = 3f;
        private const float ClearRadius = 8f;
        private const float Reach = 40f;
        private const float Cooldown = 60f;
        private const float ScanInterval = 0.25f;

        private static float quiet;
        private static float sinceScan;
        private static float readyAt = float.NegativeInfinity;

        public static float UnlockLevel => Level;
        public static float QuietSeconds => QuietTime;
        public static float ClearRange => ClearRadius;
        public static float ReachRange => Reach;
        public static float CooldownSeconds => Cooldown;

        public static void Tick(Player player, float dt)
        {
            if (!SneakSkill.On || !player.IsCrouching())
            {
                quiet = 0f;
                sinceScan = 0f;
                return;
            }
            sinceScan += dt;
            if (sinceScan < ScanInterval)
                return;
            float elapsed = sinceScan;
            sinceScan = 0f;
            HookGuard.Run("vanish", static tick => Check(tick.player, tick.elapsed), (player, elapsed));
        }

        private static void Check(Player player, float elapsed)
        {
            if (!Ready(player) || VanishScan.EnemyWithin(player, ClearRadius))
            {
                quiet = 0f;
                return;
            }
            quiet += elapsed;
            if (quiet >= QuietTime && player.IsTargeted() && VanishScan.TellHunters(player, Reach) > 0)
                Vanished(player);
        }

        private static bool Ready(Player player) =>
            Time.time >= readyAt && !player.IsDead() && !player.IsTeleporting() && LocalSkill.Reached(SneakSkill.Type, Level);

        private static void Vanished(Player player)
        {
            readyAt = Time.time + Cooldown;
            quiet = 0f;
            FloatingText.Show(player.GetTopPoint(), "Vanished");
        }
    }
}
