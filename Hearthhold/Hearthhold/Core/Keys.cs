namespace Hearthhold
{
    /// <summary>
    /// Every name Hearthhold stores or sends: RPCs, ZDO keys, player custom data keys and prefab names. All start with
    /// "hearthhold_", so they never meet another mod's or the game's.
    /// </summary>
    public static class Keys
    {
        /// <summary>RPC (float level, float stars): a star roll's inputs, actor to owner (<see cref="Marks"/>).</summary>
        public const string RpcMark = "hearthhold_mark";
    }
}
