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
        private StoneJob(Player player, ItemDrop.ItemData stone, ItemDrop.ItemData target, IRuneSupply? supply)
        {
            Player = player;
            Inventory = player.GetInventory();
            Supply = supply;
            StoneSource = supply == null ? SourceOf(Inventory, stone) : null;
            Stone = stone;
            Target = target;
            Rules = ActiveRules.Current;
            Def = Rules.Economy.StoneForPrefab(ItemTier.PrefabName(stone));
            State = ItemState.Read(target);
            Class = ItemClasses.Classify(target);
            Rarity = State.IsMagic ? State.Rarity : Rules.Economy.BaseRarity;
        }

        public Player Player { get; }

        /// <summary>The player's own inventory: where the target must be.</summary>
        public Inventory Inventory { get; }

        /// <summary>
        /// Where the rune stack lies and is paid from: the player's inventory, or the open container when this client
        /// owns it (the game hands a container to whoever opens it); null when neither holds it.
        /// </summary>
        public Inventory? StoneSource { get; }

        /// <summary>The Rune Table paying for this use instead of a carried stack; null for a click with a carried rune.</summary>
        public IRuneSupply? Supply { get; }

        /// <summary>How many of the rune this use can pay from: the carried stack, or what the table's supply holds.</summary>
        public int StonesHeld => Supply != null ? Supply.Held(Def?.Id) : Stone.m_stack;

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

        /// <summary>The socket (0-based) a gem goes into, picked in <see cref="GemChooser"/>; -1 = the next empty one.</summary>
        public int Socket { get; private set; } = -1;

        public bool IsEquipped => Player.IsItemEquiped(Target);

        public string StoneName => Words.Localize(Def?.Name ?? Stone.m_shared.m_name);

        public string ItemName => Words.Localize(Target.m_shared.m_name);

        public static StoneJob Create(Player player, ItemDrop.ItemData stone, ItemDrop.ItemData target) =>
            new StoneJob(player, stone, target, null);

        /// <summary>A use paid by a supply: <paramref name="rune"/> is the rune prefab's own item data, read only.</summary>
        public static StoneJob Create(Player player, ItemDrop.ItemData rune, ItemDrop.ItemData target, IRuneSupply supply) =>
            new StoneJob(player, rune, target, supply);

        /// <summary>The same use read afresh (after a confirm dialog, the inventory or the table may have changed).</summary>
        public StoneJob Again(Player player) => new StoneJob(player, Stone, Target, Supply) { Socket = Socket };

        /// <summary>The same use read afresh, its gem aimed at <paramref name="socket"/> (the player's pick).</summary>
        public StoneJob AtSocket(Player player, int socket) => new StoneJob(player, Stone, Target, Supply) { Socket = socket };

        private static Inventory? SourceOf(Inventory own, ItemDrop.ItemData stone)
        {
            if (own.ContainsItem(stone))
            {
                return own;
            }
            Container? open = InventoryGui.instance != null ? InventoryGui.instance.m_currentContainer : null;
            Inventory? chest = open != null && open.IsOwner() ? open.GetInventory() : null;
            return chest != null && chest.ContainsItem(stone) ? chest : null;
        }

        /// <summary>A roll context for this target under this job's rules: its class and item level, the rune's floor.</summary>
        public RollContext RollContext()
        {
            return new RollContext
            {
                Class = Class,
                Level = ItemTier.Of(Target),
                TierFloor = Def?.TierFloor ?? 0,
                Favoured = Supply?.Favoured,
                Random = RollRandom.Create(),
                Rules = Rules,
            };
        }
    }
}
