namespace Hearthhold
{
    /// <summary>The Aging Cask's names: its prefab and the ZDO key of its aging records. Both start with "hearthhold_".</summary>
    public static class CaskKeys
    {
        /// <summary>The cask's prefab name, a copy of the game's barrel.</summary>
        public const string Prefab = "hearthhold_aging_cask";

        /// <summary>ZDO byte array on a cask: its aging records, one per aging slot (<see cref="CaskRecords"/>).</summary>
        public const string Aging = "hearthhold_cask_aging";
    }
}
