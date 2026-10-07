namespace EliteCrafting.Tables.Window
{
    /// <summary>What a tab fills and does in the Rune Table's window: one each for Inscribe, Sacrifice and Sockets.</summary>
    internal abstract class TableTab
    {
        /// <summary>The tab button's word.</summary>
        public abstract string Label { get; }

        /// <summary>Whether the tab's button shows (the Sockets tab only while <c>Gems and sockets</c> is on).</summary>
        public virtual bool Shown => true;

        /// <summary>Fills the list and the description panel for the table and player now.</summary>
        public abstract void Fill(TableView view);

        /// <summary>The main button; <paramref name="all"/> is Shift held.</summary>
        public abstract void Act(TableView view, bool all);

        /// <summary>The small button under the name, where the tab shows it.</summary>
        public virtual void Extra(TableView view)
        {
        }

        /// <summary>A click on the rune row's slot.</summary>
        public virtual void PickRune(int index)
        {
        }

        /// <summary>A click on the essence row's slot.</summary>
        public virtual void PickEssence(int index)
        {
        }
    }

    /// <summary>What a tab works on: the open table, the local player, the window's parts.</summary>
    internal sealed class TableView
    {
        public TableView(RuneTable table, Player player, PanelParts parts)
        {
            Table = table;
            Player = player;
            Parts = parts;
        }

        public RuneTable Table { get; }
        public Player Player { get; }
        public PanelParts Parts { get; }

        public TableStore Store => Table.Store;
        public Inventory Inventory => Player.GetInventory();
    }
}
