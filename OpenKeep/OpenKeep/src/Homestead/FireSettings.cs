using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The fire keys of section "8. Homestead": they decide what may be built, so they are synced and locked.</summary>
    public static class FireSettings
    {
        public const string Section = HomesteadModule.Section;

        public static ConfigEntry<string> BuildOnWood { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BuildOnWood = synced.Bind(Section, "Build On Wood", "fire_pit",
                "Prefab names, comma separated, of pieces that may be built on wooden floors and other wooden pieces although the game forbids it. "
                + "The default is the campfire (fire_pit); the game has the same rule for bonfire, smelter, charcoal_kiln, blastfurnace, eitrrefinery, piece_FrostKiln and windmill. "
                + "Empty: the game's rule for every piece. Every other placement rule stays. Pieces already built stay when a name is removed. "
                + "In the Ashlands, or on a world with the Fire key, a campfire's embers set wood around it alight, a wooden floor under it too.");
        }
    }
}
