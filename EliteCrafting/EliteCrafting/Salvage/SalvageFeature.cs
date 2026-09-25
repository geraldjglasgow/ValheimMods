using EliteCrafting.Rules;

namespace EliteCrafting.Salvage
{
    /// <summary>
    /// Entry point of salvage (salvage.md): grinding a magic item into shards with the Salvage key
    /// (<see cref="SalvageKeyPatch"/>, <see cref="Grinder"/>) and fusing shards into stones by right-click
    /// (<see cref="FuseClickPatch"/>, <see cref="Fuser"/>). The five shard prefabs are registered by the Items area with
    /// the stones, on every peer, whether salvage is on or not.
    /// <para>
    /// Where it runs: only on the client whose own inventory holds the item or the shards, under the server's synced
    /// rules (the <c>Salvage</c> switch, <c>salvage:</c> yields, fuse counts, stations). No RPC: the result is ordinary
    /// inventory state that the game saves and replicates itself (salvage.md section 11).
    /// </para>
    /// </summary>
    public static class SalvageFeature
    {
        public static void Init()
        {
            // Station names resolve against the running rules; a rules change re-resolves (and re-warns) them.
            ActiveRules.RulesChanged += SalvageStations.Forget;
        }
    }
}
