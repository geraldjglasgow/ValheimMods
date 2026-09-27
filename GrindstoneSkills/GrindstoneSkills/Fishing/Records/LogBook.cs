namespace GrindstoneSkills
{
    /// <summary>
    /// Writes a catch into the angler's log (<see cref="AnglerLog"/>), on the angler's client, from
    /// <see cref="CatchHook"/>:
    /// <list type="bullet">
    /// <item>The first catch of a species is a discovery: Discovery Experience (<see cref="FishXp.OnDiscovery"/>) and "New
    /// in your angler's log: Pike (3 of 12 species)" top left.</item>
    /// <item>A known species at a level not landed before: New Size Experience and "New size in your angler's log".</item>
    /// <item>A catch heavier than the species' record replaces it, and the catch message says "new record!" (not for the
    /// first catch of a species, which is trivially the heaviest).</item>
    /// </list>
    /// </summary>
    public static class LogBook
    {
        public static void OnCatch(CatchInfo info)
        {
            Player player = info.Player;
            if (player == null || string.IsNullOrEmpty(info.Prefab))
                return;
            bool newSpecies = AnglerLog.Levels(player, info.Prefab) == 0;
            bool newSize = !AnglerLog.HasLevel(player, info.Prefab, info.Level);
            AnglerLog.AddLevel(player, info.Prefab, info.Level);
            bool record = info.Weight > 0f && AnglerLog.TryRecord(player, info.Prefab, info.Weight, info.Level);
            info.NewRecord = record && !newSpecies;
            if (newSpecies)
                Discovered(info);
            else if (newSize)
                NewSize(info);
        }

        private static void Discovered(CatchInfo info)
        {
            FishXp.OnDiscovery(info, true);
            int known = AnglerLog.SpeciesCount(info.Player);
            int total = System.Math.Max(known, FishInfo.Species().Count);
            info.Player.Message(MessageHud.MessageType.TopLeft, $"New in your angler's log: {info.Name} ({known} of {total} species)");
        }

        private static void NewSize(CatchInfo info)
        {
            FishXp.OnDiscovery(info, false);
            string size = info.Legendary ? "a legendary " + info.Name : $"a level {info.Level} {info.Name}";
            info.Player.Message(MessageHud.MessageType.TopLeft, "New size in your angler's log: " + size);
        }
    }
}
