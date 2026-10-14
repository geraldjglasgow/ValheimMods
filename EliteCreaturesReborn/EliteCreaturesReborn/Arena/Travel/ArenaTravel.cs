using System.Globalization;
using EliteCreaturesReborn.Arena.Trial;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Travel
{
    /// <summary>
    /// Getting into the hall and out again (features/arena.md section 2), on the player's own machine, for Helgrind (the
    /// gate piece and the way out at the end of the hall's gate passage, Arena/Gate) and for `elite arena`. Going in, the
    /// point to come back to is kept in the player's own custom data (<see cref="ArenaKeys.ReturnPoint"/>, saved with their
    /// character: the gate's exit point, or where the admin stood) and they are jumped to the hall's <c>arrive</c> point at
    /// once - a teleport with no loading screen, since the hall is built on this machine just before. Going out, they are
    /// jumped back to the kept point, facing as kept, with the game's normal loading, or to their bed or the world's spawn
    /// when nothing was kept. The way in is refused while Arena is off; the arena far out over the ocean is built on a
    /// machine only while its player is there, so the jump in builds it first (the hall's part of <c>Enter</c>). The
    /// way out never is refused, so nobody is left standing where a hall was. The console's cases answer in words.
    /// </summary>
    internal static class ArenaTravel
    {
        /// <summary>Jumps the local player in or out; the console's answer.</summary>
        public static string Go(Player player)
        {
            if (player.IsTeleporting())
            {
                return "elite arena: you are already on your way somewhere.";
            }
            return ArenaHall.Contains(player.transform.position) ? Out(player) : In(player);
        }

        /// <summary>Into the hall at once, keeping <paramref name="back"/> and <paramref name="facing"/> as where the way
        /// out brings the player; false when the hall cannot be built here, has no arrival point, or the game will not
        /// start the jump (one under way, or the game's two seconds between jumps). The hall is built here first when it
        /// is not yet (<see cref="ArenaHall.Ensure"/>, a frame or two), so the jump lands on a floor. The caller has checked
        /// Arena.</summary>
        public static bool Enter(Player player, Vector3 back, Quaternion facing)
        {
            if (!ArenaHall.Ensure() || !ArenaHall.TryPoint("arrive", out Vector3 arrive, out Quaternion inward)
                || !player.TeleportTo(arrive, inward, distantTeleport: false))
            {
                return false;
            }
            player.m_customData[ArenaKeys.ReturnPoint] = Write(back, facing);
            return true;
        }

        /// <summary>Back to the kept point (<paramref name="kept"/>), or to the bed or the world's spawn, with the game's
        /// loading; false when the game will not start the jump.</summary>
        public static bool Leave(Player player, out bool kept)
        {
            Vector3 back = Vector3.zero;
            Quaternion facing = player.transform.rotation;
            kept = player.m_customData.TryGetValue(ArenaKeys.ReturnPoint, out string saved)
                && TryRead(saved, ref back, ref facing);
            if (!kept)
            {
                back = SpawnPoint();
            }
            if (!player.TeleportTo(back, facing, distantTeleport: true))
            {
                return false;
            }
            player.m_customData.Remove(ArenaKeys.ReturnPoint);
            return true;
        }

        private static string In(Player player)
        {
            if (!Config.ArenaSettings.Enabled)
            {
                return "elite arena: the arena is off on this server (12 - Arena / Arena).";
            }
            return Enter(player, player.transform.position, player.transform.rotation) // builds the arena here first
                ? "elite arena: into Hel's hall. Type it again to go back."
                : "elite arena: you cannot travel right now.";
        }

        private static string Out(Player player)
        {
            if (!Leave(player, out bool kept))
            {
                return "elite arena: you cannot travel right now.";
            }
            return kept ? "elite arena: back to where you came from." : "elite arena: back to your spawn point.";
        }

        // The bed the player last slept in, else the world's spawn.
        private static Vector3 SpawnPoint()
        {
            PlayerProfile profile = Game.instance.GetPlayerProfile();
            return profile.HaveCustomSpawnPoint() ? profile.GetCustomSpawnPoint() : profile.GetHomePoint();
        }

        // "x,y,z,yaw"; a point kept before Helgrind ("x,y,z") still reads, facing as the player faces.
        private static string Write(Vector3 p, Quaternion facing) => string.Format(CultureInfo.InvariantCulture,
            "{0:R},{1:R},{2:R},{3:R}", p.x, p.y, p.z, facing.eulerAngles.y);

        private static bool TryRead(string text, ref Vector3 point, ref Quaternion facing)
        {
            string[] parts = text.Split(',');
            if (parts.Length < 3 || parts.Length > 4 || !Parse(parts[0], out float x) || !Parse(parts[1], out float y)
                || !Parse(parts[2], out float z))
            {
                return false;
            }
            point = new Vector3(x, y, z);
            if (parts.Length == 4 && Parse(parts[3], out float yaw))
            {
                facing = Quaternion.Euler(0f, yaw, 0f);
            }
            return true;
        }

        private static bool Parse(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
