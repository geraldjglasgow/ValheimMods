using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// Which sectors of the players' areas the server loads in full (everything in
    /// them, as without the setting); every other sector of a player's area gets the lone set only
    /// (<see cref="LoneSectors"/>). Full: round players in company (two whose zones are <see cref="CompanyZones"/> or
    /// fewer apart, so their creatures can be shared or soon will be: everything within <see cref="CompanyReach"/> zones
    /// of each is loaded before the server takes a creature there), round every creature the server runs (one zone round
    /// it, so it collides, paths and picks targets as on a player's game), and round boss altars (the game checks a boss's
    /// spawn point against the buildings and trees round the altar). Marked by the ownership pass every two seconds
    /// (<see cref="SharedRule"/>) and held <see cref="HoldSeconds"/> after the last mark, so a group that parts for a moment
    /// does not unload and reload its bases. A sector whose hold has run out drops to the lone set at most
    /// <see cref="DropsPerTick"/> per load tick: the game destroys every unlisted instance at once, and a base's sectors
    /// together hold thousands. Two times per sector (full until, last loaded in full), for the game's whole 512 by 512
    /// sector grid.
    /// </summary>
    internal static class FullSectors
    {
        /// <summary>Seconds a sector stays full after the last reason for it.</summary>
        public const float HoldSeconds = 20f;

        /// <summary>Sectors dropped from full to the lone set in one load tick (30 a second).</summary>
        public const int DropsPerTick = 1;

        /// <summary>Zones out from a player in company that are loaded in full.</summary>
        public const int CompanyReach = 2;

        // A sector loaded in full no later than this long ago still holds its instances (the load ticks run 30 a second).
        private const float StillLoadedSeconds = 0.5f;

        private const int Width = 512;
        private static readonly float[] Until = new float[Width * Width];
        private static readonly float[] LoadedAt = new float[Width * Width];
        private static int _drops;

        /// <summary>Two players are in company when their zones are this many apart or fewer: their active areas meet at
        /// <see cref="NearPlayers.Reach"/> zones apart, and one zone more loads the buildings between them before the
        /// first creature there is shared.</summary>
        public static int CompanyZones(SimulationDistance sim) => NearPlayers.Reach(sim) + 1;

        /// <summary>A new load tick: the budget of sectors that may drop to the lone set is refilled.</summary>
        public static void NewTick() => _drops = DropsPerTick;

        /// <summary>Marks every sector within <paramref name="radius"/> zones of <paramref name="zone"/> full from now.</summary>
        public static void MarkAround(Vector2s zone, int radius, float now)
        {
            float until = now + HoldSeconds;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    uint index = ZoneSystem.SectorToIndex(zone.x + dx, zone.y + dy).Sector;
                    if (index != 0)
                    {
                        Until[index] = until;
                    }
                }
            }
        }

        /// <summary>True when the sector is loaded in full this tick (and remembered as loaded so).</summary>
        public static bool IsFull(uint index, float now)
        {
            if (Until[index] > now)
            {
                LoadedAt[index] = now;
                return true;
            }
            if (now - LoadedAt[index] > StillLoadedSeconds)
            {
                return false; // not loaded in full lately: nothing to unload
            }
            if (_drops <= 0)
            {
                LoadedAt[index] = now;
                return true; // its turn to drop comes on a later tick
            }
            _drops--;
            LoadedAt[index] = float.MinValue;
            return false;
        }
    }
}
