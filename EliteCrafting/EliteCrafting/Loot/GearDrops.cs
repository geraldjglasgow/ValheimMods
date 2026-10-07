namespace EliteCrafting.Loot
{
    /// <summary>
    /// Whether creatures and chests drop pre-rolled magic gear: never, for now (user decision 2026-10-07: "for now don't
    /// allow elitecrafting gear to drop at all"; the setting <c>Magic item drops</c> went with it). Magic and Rare items
    /// come only from runes. The gear path (<see cref="LootPlanner"/>, <see cref="GearFactory"/>) stays for when it comes
    /// back.
    /// </summary>
    internal static class GearDrops
    {
        public const bool On = false;
    }
}
