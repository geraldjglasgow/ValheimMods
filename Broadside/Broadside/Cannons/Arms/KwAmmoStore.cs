using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// What a cannon's reload takes its balls and powder from (<see cref="KwArmReload"/>), counted and taken by the item's
    /// name as the game keeps it in inventories: a player's pack (<see cref="KwPackStore"/>: the gunner's, or the captain's
    /// for his broadside's reloads, <see cref="KwBroadsideReload.Store"/>), or a tally over another store counting off what
    /// earlier loads would take (<see cref="KwAmmoTally"/>, the broadside's forecast). A ship's chest would be one more.
    /// </summary>
    public abstract class KwAmmoStore
    {
        private static readonly KwPackStore Shared = new KwPackStore(null);

        /// <summary>How many of the item it holds.</summary>
        public abstract int Count(string name);

        /// <summary>Takes so many of the item.</summary>
        public abstract void Remove(string name, int amount);

        /// <summary>A player's pack, one reused store (for the gunner's reload, each physics step; never kept).</summary>
        public static KwAmmoStore Pack(Player player)
        {
            Shared.Inventory = player.GetInventory();
            return Shared;
        }
    }

    /// <summary>A player's inventory as a store: the game's own counting and taking.</summary>
    public sealed class KwPackStore : KwAmmoStore
    {
        public KwPackStore(Inventory? inventory) => Inventory = inventory;

        public Inventory? Inventory { get; set; }

        public override int Count(string name) => Inventory != null ? Inventory.CountItems(name) : 0;

        public override void Remove(string name, int amount) => Inventory?.RemoveItem(name, amount);
    }

    /// <summary>A store's counts less what has been taken from the tally: nothing is taken from the store itself.</summary>
    public sealed class KwAmmoTally : KwAmmoStore
    {
        private readonly KwAmmoStore source;
        private readonly Dictionary<string, int> taken = new Dictionary<string, int>();

        public KwAmmoTally(KwAmmoStore source) => this.source = source;

        public override int Count(string name) => source.Count(name) - (taken.TryGetValue(name, out int gone) ? gone : 0);

        public override void Remove(string name, int amount) => taken[name] = (taken.TryGetValue(name, out int gone) ? gone : 0) + amount;
    }
}
