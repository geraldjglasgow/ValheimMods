using EliteCrafting.Affixes;
using EliteCrafting.Items;
using EliteCrafting.Rolling;
using EliteCrafting.Rules;
using EliteCrafting.Text;

namespace EliteCrafting.Stones
{
    /// <summary>
    /// Everything one stone use reads, gathered once per click on the local client: the player, the stone stack and
    /// the target, the rules snapshot (the server's synced rules while it binds), the stone's definition, the target's
    /// state, slot, current rarity and pending sigil. Read-only; the pipeline and the verbs decide from it.
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
            Slot = ItemSlots.Classify(target);
            Rarity = State.IsMagic ? State.Rarity : Rules.Economy.BaseRarity;
            Sigil = PendingSigil.Resolve(State, Rules);
        }

        public Player Player { get; }
        public Inventory Inventory { get; }
        public ItemDrop.ItemData Stone { get; }
        public ItemDrop.ItemData Target { get; }
        public RuleSet Rules { get; }

        /// <summary>The live definition bound to the stone's prefab; null when the YAML has none.</summary>
        public StoneDef? Def { get; }

        public ItemState State { get; }
        public SlotInfo Slot { get; }

        /// <summary>The target's rarity before the stone acts (the base rarity for Common); null when unknown.</summary>
        public RarityDef? Rarity { get; }

        public PendingSigil Sigil { get; }

        /// <summary>Stones this use costs, paid for the rarity before the stone acts (default 1; 0 is free).</summary>
        public int Cost => Def != null && Rarity != null ? System.Math.Max(Def.CostFor(Rarity.Id), 0) : 1;

        public bool IsEquipped => Player.IsItemEquiped(Target);

        public string StoneName => Words.Localize(Def?.Name ?? Stone.m_shared.m_name);

        public string ItemName => Words.Localize(Target.m_shared.m_name);

        public static StoneJob Create(Player player, ItemDrop.ItemData stone, ItemDrop.ItemData target) =>
            new StoneJob(player, stone, target);

        /// <summary>A roll context for this target under this job's rules, with the stone's floor and an optional steer.</summary>
        public RollContext RollContext(AffixCategory? category)
        {
            return new RollContext
            {
                Slot = Slot,
                Ceiling = ItemTier.Of(Target),
                TierFloor = Def?.TierFloor ?? 0,
                Category = category,
                Random = RollRandom.Create(),
                Rules = Rules,
            };
        }
    }
}
