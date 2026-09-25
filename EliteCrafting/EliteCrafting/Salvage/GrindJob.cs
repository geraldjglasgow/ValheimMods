using System.Collections.Generic;
using EliteCrafting.Affixes;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Salvage
{
    /// <summary>
    /// Everything one grind reads, gathered once per key press on the local client: the player, the hovered item, its
    /// state and rarity, the rules snapshot (the server's while it binds) and the rarity's yield rows. Read-only.
    /// </summary>
    internal sealed class GrindJob
    {
        public GrindJob(Player player, ItemDrop.ItemData item)
        {
            Player = player;
            Inventory = player.GetInventory();
            Item = item;
            Rules = ActiveRules.Current;
            State = ItemState.Read(item);
            Rarity = State.Rarity;
            Rows = Rarity != null ? Rules.Economy.Salvage.YieldOf(Rarity.Id) : System.Array.Empty<SalvageYield>();
        }

        public Player Player { get; }
        public Inventory Inventory { get; }
        public ItemDrop.ItemData Item { get; }
        public RuleSet Rules { get; }
        public ItemState State { get; }

        /// <summary>The item's rarity; null for a plain item or an unknown rarity.</summary>
        public RarityDef? Rarity { get; }

        public IReadOnlyList<SalvageYield> Rows { get; }

        public SalvageRules Salvage => Rules.Economy.Salvage;

        public string ItemName => Words.Localize(Item.m_shared.m_name);

        /// <summary>Per shard id, what every row pays together if every chance succeeds (the fit check's worst case).</summary>
        public Dictionary<string, int> Totals()
        {
            Dictionary<string, int> totals = new Dictionary<string, int>(System.StringComparer.Ordinal);
            foreach (SalvageYield row in Rows)
            {
                totals.TryGetValue(row.Fragment, out int sum);
                totals[row.Fragment] = sum + row.Amount;
            }
            return totals;
        }
    }
}
