namespace OpenKeep.Homestead
{
    /// <summary>
    /// The checks the bed patches make on one bed. The owner is the game's own ZDO key <c>owner</c> (the claiming
    /// player's id, written by the bed's ZDO owner in <c>Bed.RPC_SetOwner</c>), compared with the local profile's
    /// player id as <c>Bed.IsMine</c> does. <see cref="IsLive"/>: a real bed in the world, not a placement ghost (no
    /// ZDO). <see cref="IsSpawnPoint"/>: the profile has a spawn point and it is this bed's; stricter than
    /// <c>Bed.IsCurrent</c>, which also matches the stale point the game leaves behind when it clears one.
    /// </summary>
    public static class BedChecks
    {
        public static bool IsLive(Bed bed)
        {
            return bed != null && bed.m_spawnPoint != null && bed.m_nview != null && bed.m_nview.GetZDO() != null;
        }

        public static bool IsUnclaimed(Bed bed) => Owner(bed) == 0L;

        public static bool IsLocal(Bed bed)
        {
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            long owner = Owner(bed);
            return profile != null && owner != 0L && owner == profile.GetPlayerID();
        }

        public static bool IsSpawnPoint(Bed bed)
        {
            PlayerProfile profile = Game.instance != null ? Game.instance.GetPlayerProfile() : null;
            return profile != null && profile.HaveCustomSpawnPoint()
                && BedPoints.Same(profile.GetCustomSpawnPoint(), bed.GetSpawnPoint());
        }

        private static long Owner(Bed bed) => bed.m_nview.GetZDO().GetLong(ZDOVars.s_owner, 0L);
    }
}
