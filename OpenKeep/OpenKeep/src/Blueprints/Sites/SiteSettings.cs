using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Blueprints.Sites
{
    /// <summary>
    /// The construction sites' one setting, in section "14. Blueprints" beside the feature's switch: whether a site builds
    /// piece by piece as its materials arrive or waits until it has everything. Synced, so the server decides for every
    /// builder; read at use time.
    /// </summary>
    public static class SiteSettings
    {
        public static ConfigEntry<bool> PieceByPieceEntry { get; private set; }

        /// <summary>A site builds as its materials come in (on, the default), not only once it has them all.</summary>
        public static bool PieceByPiece => PieceByPieceEntry == null || PieceByPieceEntry.Value;

        public static void Bind(SyncedConfiguration synced)
        {
            PieceByPieceEntry = synced.Bind(BlueprintSettings.Section, "Build As Resources Come In", true,
                "When on, a construction site (the second click with a blueprint from the hammer's Blueprints tab) builds as materials are handed " +
                "to it: first the ground once its Stone is there, then the queued selections in order, then the rest in the blueprint's " +
                "order, eight pieces a second, waiting whenever the next piece's materials are not there yet. Off: nothing is " +
                "built until the site holds everything it still needs, then the ground and every piece go up at once. With " +
                "Build Without Materials on (or a builder in no-cost mode) a site builds at once either way.");
        }
    }
}
