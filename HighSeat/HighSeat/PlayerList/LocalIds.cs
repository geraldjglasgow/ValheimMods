using Splatform;

namespace HighSeat
{
    /// <summary>
    /// The platform's local user id (PlatformManager.DistributionPlatform.LocalUser.PlatformUserID), which the game reads
    /// at every player list and history update; on Steam each read writes the Steam ID out as a new string. It does not
    /// change while the game runs once it is valid, so it is read until valid and then kept for the session (a new world
    /// reads it again).
    /// </summary>
    internal static class LocalIds
    {
        private static PlatformUserID _id;
        private static bool _have;

        public static void Reset() => _have = false;

        public static PlatformUserID Id()
        {
            if (_have)
            {
                return _id;
            }
            PlatformUserID id = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
            if (id.IsValid)
            {
                _id = id;
                _have = true;
            }
            return id;
        }
    }
}
