using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// What the captain's broadside loads a cannon from, on his game (<see cref="KwBroadsideReload"/>): what piles have
    /// already handed over (<see cref="KwPileCredit"/>), then the ammunition piles riding the cannon's own ship
    /// (<see cref="KwPile.OnShip"/>: never another ship's), then his pack, so the balls come from the cannonball piles and
    /// the powder from the powder piles first and only what they are short of from his pack. Two ways:
    /// <see cref="Plan"/> counts every pile aboard as this game has its inventory and takes nothing, noting what would be
    /// asked of the piles another peer owns (<see cref="Asks"/>, sent by <see cref="KwPileAsk"/>), and is the broadside's
    /// forecast too; <see cref="Take"/> takes, from what was handed over, the piles this game owns (it writes only its own
    /// ZDOs) and his pack. A load names each item once (its powder and its ball are different items).
    /// </summary>
    public sealed class KwPileStore : KwAmmoStore
    {
        private readonly List<KwPile> owned = new List<KwPile>(), others = new List<KwPile>();
        private readonly Inventory? pack;
        private readonly bool planning;

        private KwPileStore(KwShipRiders? ship, Player captain, bool planning)
        {
            pack = captain.GetInventory();
            this.planning = planning;
            if (ship == null)
                return;
            KwPile.OnShip(ship, owned, true);
            if (planning)
                KwPile.OnShip(ship, others, false);
        }

        /// <summary>What the piles another peer owns would be asked for (a pile, the item's name, how many), noted while planning.</summary>
        public List<(KwPile Pile, string Item, int Amount)> Asks { get; } = new List<(KwPile, string, int)>();

        /// <summary>Counting every pile aboard, taking nothing: what a load would ask of the piles another peer owns.</summary>
        public static KwPileStore Plan(KwShipRiders? ship, Player captain) => new KwPileStore(ship, captain, true);

        /// <summary>Taking: what was handed over, then the piles this game owns, then the captain's pack.</summary>
        public static KwPileStore Take(KwShipRiders? ship, Player captain) => new KwPileStore(ship, captain, false);

        public override int Count(string name)
        {
            int count = KwPileCredit.Count(name) + (pack != null ? pack.CountItems(name) : 0);
            foreach (KwPile pile in owned)
                count += pile.Count(name);
            foreach (KwPile pile in others)
                count += pile.Count(name);
            return count;
        }

        public override void Remove(string name, int amount)
        {
            if (planning)
            {
                Ask(name, amount);
                return;
            }
            amount -= KwPileCredit.Spend(name, amount);
            foreach (KwPile pile in owned)
                amount -= pile.Take(name, amount);
            if (amount > 0)
                pack?.RemoveItem(name, amount);
        }

        // What is not handed over yet nor in this game's own piles, asked of the others in turn while they hold any.
        private void Ask(string name, int amount)
        {
            amount -= KwPileCredit.Count(name);
            foreach (KwPile pile in owned)
                amount -= pile.Count(name);
            foreach (KwPile pile in others)
            {
                int asked = System.Math.Min(amount, pile.Count(name));
                if (asked <= 0)
                    continue;
                Asks.Add((pile, name, asked));
                amount -= asked;
            }
        }
    }
}
