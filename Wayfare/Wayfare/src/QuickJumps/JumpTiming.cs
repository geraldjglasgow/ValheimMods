using AreaLoading;
using UnityEngine;
using Wayfare.Core;
using Wayfare.SeaGates;

namespace Wayfare.QuickJumps
{
    /// <summary>Quick Portals. A long jump (<c>Player.TeleportTo</c> with <c>distantTeleport</c>, what portals, Wayfare's
    /// map targeting and the console's goto use; dungeon doors are short jumps) runs on the jumping player's own client
    /// in <c>Player.UpdateTeleport</c>: the player moves when <c>m_teleportTimer</c> passes 2 s and lands once it passes
    /// 8 s and the area is ready. The jump's seconds come from the map distance between its start and its target. The
    /// move comes early, after at most <see cref="PreMoveMax"/> (the screen is black by then, <see cref="JumpScreen"/>),
    /// because the target area only starts loading once the player is there; the rest of the seconds run from the move
    /// to the landing. The timer is never pushed past 8 s, so the game's area check and its 15 s no-floor fallback stay.
    /// On a client of a server the landing also waits for the target's objects to have arrived
    /// (<see cref="AreaSettle"/>), but never beyond the game's own 8 s from the start. A sea gate jump is left alone:
    /// its crew hold runs the teleport itself (<see cref="CrewHold"/>).</summary>
    public static class JumpTiming
    {
        /// <summary>The game's seconds before a long jump may land.</summary>
        public const float GameArrival = 8f;

        /// <summary>The game's timer value at which the player is moved to the target.</summary>
        private const float MoveAt = 2f;

        /// <summary>Real seconds from stepping in to the move at most.</summary>
        private const float PreMoveMax = 0.25f;

        private static float startedAt;

        public static bool Applies => WayfareConfig.Enabled.Value && WayfareConfig.QuickPortals.Value && !CrewHold.Holding;

        public static void Hurry(Player player, float dt)
        {
            if (!Applies || player.m_teleportTimer >= GameArrival)
                return;
            float seconds = Seconds(player);
            if (seconds >= GameArrival)
                return;
            float preMove = Mathf.Min(PreMoveMax, seconds * 0.25f);
            if (player.m_teleportTimer < MoveAt)
                Advance(player, dt, preMove, 0f, MoveAt);
            else
                Advance(player, dt, seconds - preMove, MoveAt, GameArrival);
            Hold(player, dt);
        }

        private static float Seconds(Player player)
        {
            float metres = QuickWait.MapDistance(player.m_teleportFromPos, player.m_teleportTargetPos);
            float seconds = QuickWait.Seconds(metres, WayfareConfig.QuickPortalRange.Value, WayfareConfig.QuickPortalSeconds.Value, GameArrival);
            if (player.m_teleportTimer <= 0f)
            {
                startedAt = Time.time;
                Plugin.Log.LogInfo($"Wayfare: jump of {metres:F0} m takes {seconds:0.#} s (the game's {GameArrival:0} s), once the area is loaded");
            }
            return seconds;
        }

        /// <summary>Runs the timer from <paramref name="from"/> to <paramref name="to"/> in <paramref name="seconds"/>, the game adding its own tick.</summary>
        private static void Advance(Player player, float dt, float seconds, float from, float to)
        {
            float extra = dt * (QuickWait.Speed(seconds, to - from) - 1f);
            player.m_teleportTimer = Mathf.Min(player.m_teleportTimer + extra, to);
        }

        /// <summary>After the move, keeps the jump from landing while the server is still sending the target's objects.</summary>
        private static void Hold(Player player, float dt)
        {
            if (player.m_teleportTimer < MoveAt || Time.time - startedAt >= GameArrival || AreaSettle.Settled(player.m_teleportTargetPos))
                return;
            player.m_teleportTimer = Mathf.Min(player.m_teleportTimer, GameArrival - dt * 1.5f);
        }
    }
}
