using AreaLoading;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// Quick Respawn: how long the local player lies dead. The game waits 10 s after death (<c>Player.OnDeath</c> calls
    /// <c>RequestRespawn(10f)</c>), then <c>m_respawnLoadDuration</c> (8 s) in <c>Game.FindSpawnPoint</c> until it
    /// looks for the bed, which also waits for the bed's area to load. Both parts shrink by the same share, taken from
    /// the map distance between the death point and where the profile says the player wakes (the bed, or the world
    /// start without one). The death part is a new <c>RequestRespawn</c> delay counted from the death; the loading
    /// part runs the game's <c>m_respawnWait</c> faster (<see cref="BedLoadPatch"/>). Off: the game's wait.
    /// </summary>
    public static class BedWait
    {
        /// <summary>The game's delay between death and the respawn request.</summary>
        public const float GameDeathDelay = 10f;

        private static Vector3? deathPoint;
        private static float deathTime;
        private static float loadStartedAt;

        public static void Died(Vector3 point)
        {
            deathPoint = point;
            deathTime = Time.time;
        }

        /// <summary>The player spawned: the next wait belongs to the next death.</summary>
        public static void Clear() => deathPoint = null;

        /// <summary>
        /// Asks the game to respawn once the death part of the wait for the current bed is over, counted from the
        /// death, and not before <paramref name="atLeast"/> seconds from now. Replaces the game's pending request.
        /// </summary>
        public static void Schedule(float atLeast = 0f)
        {
            if (Game.instance == null || !deathPoint.HasValue)
                return;
            float full = Full();
            float seconds = Seconds(full);
            float delay = Mathf.Max(atLeast, GameDeathDelay * seconds / full - (Time.time - deathTime));
            Game.instance.RequestRespawn(Mathf.Max(0f, delay), afterDeath: true);
            if (seconds < full)
                Plugin.Log.LogInfo($"OpenKeep: waking in {seconds:0.#} s after death (the game waits {full:0.#} s), respawn requested in {delay:0.#} s");
        }

        /// <summary>How many times faster than the game the loading part runs for the current bed; 1 when off.</summary>
        public static float LoadSpeed()
        {
            float full = Full();
            return QuickWait.Speed(Seconds(full), full);
        }

        /// <summary>
        /// On a client of a server, keeps the game from looking for the bed while the server is still sending the
        /// objects around it (<see cref="AreaSettle"/>): found too early, the bed is not there yet, and the game would
        /// clear it and wake the player elsewhere. Never beyond the game's own load wait, counted from when it began
        /// (<paramref name="fresh"/>: <c>m_respawnWait</c> was 0, as after the respawn request and for each next bed).
        /// </summary>
        public static void Hold(Game game, float dt, Vector3 bed, bool fresh)
        {
            if (fresh)
                loadStartedAt = Time.time;
            if (Time.time - loadStartedAt >= game.m_respawnLoadDuration || AreaSettle.Settled(bed))
                return;
            game.m_respawnWait = Mathf.Min(game.m_respawnWait, game.m_respawnLoadDuration - dt * 1.5f);
        }

        private static float Full() => GameDeathDelay + Game.instance.m_respawnLoadDuration;

        /// <summary>
        /// Quick Area Loading's reason (registered with <see cref="AreaLoader"/>): the local player is dead and the game
        /// waits for the respawn, loading the land around the bed.
        /// </summary>
        public static bool LoadingLand()
        {
            return BedSettings.QuickAreaLoading.Value && Player.m_localPlayer == null && Game.instance != null
                && Game.instance.WaitingForRespawn();
        }

        /// <summary>The whole wait from death to waking for the current target; the game's <paramref name="full"/> when off or unknown.</summary>
        private static float Seconds(float full)
        {
            if (!BedSettings.QuickRespawn.Value || !deathPoint.HasValue || !TryTarget(out Vector3 target))
                return full;
            float metres = BedPoints.MapDistance(deathPoint.Value, target);
            return QuickWait.Seconds(metres, BedSettings.QuickRespawnRange.Value, BedSettings.QuickRespawnSeconds.Value, full);
        }

        /// <summary>Where the game will put the player: the profile's bed, else the world start as <c>FindSpawnPoint</c> finds it.</summary>
        private static bool TryTarget(out Vector3 target)
        {
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            if (profile != null && profile.HaveCustomSpawnPoint())
            {
                target = profile.GetCustomSpawnPoint();
                return true;
            }
            target = Vector3.zero;
            return ZoneSystem.instance != null && ZoneSystem.instance.GetLocationIcon(Game.instance.m_StartLocation, out target);
        }
    }
}
