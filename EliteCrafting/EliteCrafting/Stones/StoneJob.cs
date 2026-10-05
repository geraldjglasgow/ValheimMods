using EliteCrafting.Affixes;
using EliteCrafting.Items;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// Everything one rune use reads, gathered once per click on the local client: the player, the rune stack and
    /// the target, the rules snapshot (the server's synced rules while it binds), the rune's definition, the target's
    /// state, item class and current rarity. Read-only; the pipeline and the verbs decide from it.
    /// </summary>
    internal sealed class StoneJob
    {
        private StoneJob(Player player, ItemDrop.ItemData stone, ItemDrop.ItemData target)
        {
            Player = player;
            Inventory = player.GetInventory();
            Stone = stone;
            Target = target;
            Rules = ActiveRules.Current;
            Def = Rules.Economy.StoneForPrefab(ItemTier.PrefabName(stone));
            State = ItemState.Read(target);
            Class = ItemClasses.Classify(target);
            Rarity = State.IsMagic ? State.Rarity : Rules.Economy.BaseRarity;
        }

        public Player Player { get; }
        public Inventory Inventory { get; }
        public ItemDrop.ItemData Stone { get; }
        public ItemDrop.ItemData Target { get; }
        public RuleSet Rules { get; }

        /// <summary>The live definition bound to the stone's prefab; null when the YAML has none.</summary>
        public StoneDef? Def { get; }

        public ItemState State { get; }

        /// <summary>The target's item class with its hands, traits and skills.</summary>
        public ClassInfo Class { get; }

        /// <summary>The target's rarity before the rune acts (the base rarity for Normal); null when unknown.</summary>
        public RarityDef? Rarity { get; }

        /// <summary>Stones this use costs, paid for the rarity before the stone acts (default 1; 0 is free).</summary>
        public int Cost => Def != null && Rarity != null ? System.Math.Max(Def.CostFor(Rarity.Id), 0) : 1;

        public bool IsEquipped => Player.IsItemEquiped(Target);

        public string StoneName => Words.Localize(Def?.Name ?? Stone.m_shared.m_name);

        public string ItemName => Words.Localize(Target.m_shared.m_name);

        public static StoneJob Create(Player player, ItemDrop.ItemData stone, ItemDrop.ItemData target) =>
            new StoneJob(player, stone, target);

        /// <summary>A roll context for this target under this job's rules: its class and item level, the rune's floor.</summary>
        public RollContext RollContext()
        {
            return new RollContext
            {
                Class = Class,
                Level = ItemTier.Of(Target),
                TierFloor = Def?.TierFloor ?? 0,
                Random = RollRandom.Create(),
                Rules = Rules,
            };
        }
    }
}
