namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// What a raider goes for once it has no player to fight, given when it spawns, stored as an int in its tag
    /// (<see cref="RaidKeys.RaiderRole"/>). Saved with the world, so the numbers never change meaning.
    /// </summary>
    public enum RaiderRole
    {
        /// <summary>Not a raider.</summary>
        None = 0,

        /// <summary>Goes for the Raiders Chest; becomes a plunderer when it is gone (or the raid has no chest).</summary>
        ChestRaider = 1,

        /// <summary>Goes for the base's nearest chest, then its crafting stations.</summary>
        Plunderer = 2,
    }
}
