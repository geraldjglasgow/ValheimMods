using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Domino felling (section 11): how much harder a felled log hits other wood, growing linearly from nothing at
    /// level 0 to its value at level 100, and how far down a chain that reaches. Synced.
    /// </summary>
    public static class DominoSettings
    {
        public const string Section = WoodcuttingSettings.FellingSection;

        public static ConfigEntry<float> ImpactAt100 { get; private set; }
        public static ConfigEntry<int> MaxChain { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            ImpactAt100 = config.Bind(Section, "Domino Impact At 100", 200f,
                "Percent more damage a tree felled by a level 100 woodcutter deals when it falls onto other trees, logs and stumps, so it can knock them over in turn. Players, creatures and buildings take the game's own damage.",
                acceptableValues: Settings.UpTo(1000f));
            MaxChain = config.Bind(Section, "Domino Max Chain", 5,
                "How many trees in a row the harder impacts reach: the first tree a felled one knocks over is 1. Further down the chain, logs hit with the game's own force.",
                acceptableValues: new AcceptableValueRange<int>(1, 50));
        }
    }
}
