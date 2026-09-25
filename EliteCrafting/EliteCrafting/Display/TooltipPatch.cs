using EliteCrafting.Affixes;
using HarmonyLib;

namespace EliteCrafting.Display
{
    /// <summary>
    /// Appends the affix block after the vanilla tooltip (display.md section 3): a postfix on the static
    /// <c>ItemData.GetTooltip(ItemData, int, bool, float, int, bool)</c>, skipped for appended sub-tooltips and for items
    /// with no <c>ecf_</c> data. Covers the inventory and container grids, the trader and the radial menu. The crafting
    /// panel passes the recipe prefab's data: while it describes an upgrade, <see cref="CraftingPanel.Shown"/> swaps in
    /// the player's item being upgraded, so its block appears under the recipe (display.md section 2).
    /// Runs on the viewing client, every frame while an item is hovered: one cache read, one block lookup, and a
    /// two-slot memo so the concatenation is not redone while the vanilla text stays the same.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    internal static class TooltipPatch
    {
        private static readonly string?[] LastVanilla = new string?[2];
        private static readonly string?[] LastBlock = new string?[2];
        private static readonly string?[] LastResult = new string?[2];
        private static int _next;

        [HarmonyPostfix]
        private static void Postfix(ItemDrop.ItemData item, bool crafting, bool appending, ref string __result)
        {
            if (appending || item == null || __result == null)
            {
                return;
            }
            ItemDrop.ItemData shown = crafting ? CraftingPanel.Shown(item) : item;
            ItemState state = ItemState.Read(shown);
            if (state.IsEmpty)
            {
                return;
            }
            string block = DisplayCache.Block(state, shown);
            if (block.Length > 0)
            {
                __result = Join(__result, block);
            }
        }

        /// <summary>vanilla + block, reusing the previous result when both are unchanged (content compare, no allocation).</summary>
        private static string Join(string vanilla, string block)
        {
            for (int i = 0; i < LastResult.Length; i++)
            {
                if (ReferenceEquals(LastBlock[i], block) && string.Equals(LastVanilla[i], vanilla))
                {
                    return LastResult[i]!;
                }
            }
            int slot = _next;
            _next = (_next + 1) % LastResult.Length;
            LastVanilla[slot] = vanilla;
            LastBlock[slot] = block;
            LastResult[slot] = vanilla + block;
            return LastResult[slot]!;
        }
    }
}
