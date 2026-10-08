using EliteCrafting.Stones;

namespace EliteCrafting.Tables.Window
{
    /// <summary>
    /// Whether the window's main button would do something (user 2026-10-07: the buttons "should only be unclickable and
    /// grey if you can't use the items selected"): the same checks and dry run a press runs (<see cref="StonePipeline"/>,
    /// paid by a <see cref="TableSupply"/>), nothing written. A gem for a full item counts as usable: the press asks for
    /// the socket.
    /// </summary>
    internal static class TableUse
    {
        public static bool Works(TableView view, ItemDrop.ItemData? target, string stoneId, Essence? essence)
        {
            ItemDrop.ItemData? stone = TableIcons.RuneItem(stoneId);
            if (target == null || stone == null || !view.Store.Writable)
            {
                return false;
            }
            StoneJob job = StoneJob.Create(view.Player, stone, target, new TableSupply(view.Store, essence));
            return !StonePipeline.Evaluate(job).Refused;
        }
    }
}
