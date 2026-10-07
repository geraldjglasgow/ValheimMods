using BepInEx.Configuration;
using SyncedConfig;

namespace EliteCreaturesPack.BearClaws
{
    /// <summary>
    /// Section 27: the game's bear claws (Paws of the Bear), one switch for their triple strike
    /// (<see cref="BearClawFlurry"/>). Synced; read at each punch, so a reload takes effect at once.
    /// </summary>
    public static class BearClawsSettings
    {
        public const string Section = "27 - Bear Claws";

        private static ConfigEntry<bool> tripleStrike = null!;

        /// <summary>Whether a punch with the bear claws is a flurry of three swipes.</summary>
        public static bool TripleStrike => tripleStrike.Value;

        public static void Initialize(SyncedConfiguration config)
        {
            tripleStrike = config.Bind(Section, "Triple Strike", true,
                "The game's Paws of the Bear: each punch is three swipes, left right left or right left right, in the time "
                + "of one punch, each doing about a third of the punch's damage. Stamina, skill gain, durability and adrenaline "
                + "stay as they were. Off: the game's single punch.");
        }
    }
}
