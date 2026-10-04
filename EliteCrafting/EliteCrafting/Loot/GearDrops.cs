using EliteCrafting.Config;
using EliteCrafting.Epic;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// Whether creatures and chests drop pre-rolled magic gear: the synced <c>Magic item drops</c> switch, and never while
    /// Epic Loot is installed (the user's decision 2026-10-03: Epic Loot's own magic items take their place; the runes
    /// still drop and work on them). Read on the peer that rolls the loot.
    /// </summary>
    internal static class GearDrops
    {
        public static bool On => ModSettings.MagicItemDrops.Value && !EpicApi.Installed;
    }
}
