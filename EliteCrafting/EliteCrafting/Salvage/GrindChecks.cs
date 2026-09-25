using EliteCrafting.Config;
using EliteCrafting.Items;
using EliteCrafting.Stones;

namespace EliteCrafting.Salvage
{
    /// <summary>
    /// The checks of salvage.md section 3, steps 1-10, in order; the first failure refuses and nothing changes. Every
    /// step only reads the item, the player's own inventory and the synced rules, so a host and a client on a dedicated
    /// server refuse identically. The confirm gate (11) follows in <see cref="Grinder"/>. Local client.
    /// </summary>
    internal static class GrindChecks
    {
        public static StoneMessage? Evaluate(GrindJob job) => Access(job) ?? ItemChecks(job) ?? YieldChecks(job);

        // 1-2: the synced switch; the item is in the local player's own inventory.
        private static StoneMessage? Access(GrindJob job)
        {
            if (!(ModSettings.SalvageEnabled?.Value ?? true))
            {
                return Refuse("salvage_disabled");
            }
            return job.Inventory.ContainsItem(job.Item) ? null : Refuse("salvage_not_own");
        }

        // 3-7: a magic item of ours above the base rarity (never a stone, essence or shard: stackable), not a newer
        // format, a known rarity, not equipped (SAL-8: always), no live sigil pending (SAL-7; a dormant one is inert, IMP-105).
        private static StoneMessage? ItemChecks(GrindJob job)
        {
            string? baseId = job.Rules.Economy.BaseRarity?.Id;
            if (!ItemSlots.IsMagicBase(job.Item) || !job.State.IsMagic || job.State.RarityId == baseId)
            {
                return Refuse("salvage_not_magic");
            }
            if (job.State.IsNewerFormat)
            {
                return Refuse("newer_format");
            }
            if (job.Rarity == null)
            {
                return Refuse("unknown_rarity");
            }
            if (job.Player.IsItemEquiped(job.Item))
            {
                return Refuse("equipped");
            }
            return PendingSigil.Resolve(job.State, job.Rules).IsLive ? Refuse("salvage_sigil_pending") : null;
        }

        // 8-10: the rarity pays something, a listed station is near, and the shards fit once the item is gone.
        private static StoneMessage? YieldChecks(GrindJob job)
        {
            if (job.Rows.Count == 0)
            {
                return Refuse("salvage_no_yield", StoneNames.Rarity(job.Rarity!));
            }
            if (!SalvageStations.InRange(job.Salvage.Stations, job.Player.transform.position))
            {
                return Refuse("salvage_station", SalvageStations.FirstName(job.Salvage.Stations));
            }
            return ShardRoom.Fits(job.Inventory, job.Totals()) ? null : Refuse("salvage_no_room");
        }

        private static StoneMessage Refuse(string id, params string[] words) => new StoneMessage(id, words);
    }
}
