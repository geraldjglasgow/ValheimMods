using EliteCrafting.Config;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// Whether creatures and chests drop pre-rolled magic gear: the synced <c>Magic item drops</c> switch. Read on the
    /// peer that rolls the loot.
    /// </summary>
    internal static class GearDrops
    {
        public static bool On => ModSettings.MagicItemDrops.Value;
    }
}
