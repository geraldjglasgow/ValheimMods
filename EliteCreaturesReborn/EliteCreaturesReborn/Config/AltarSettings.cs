using BepInEx.Configuration;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// The altar choice's one setting: whether a player at a boss altar may choose the boss's stars and aspect, each part
    /// left on Random rolling at every shift as before. It changes the game, so like the arena's switch
    /// (<see cref="ArenaSettings"/>) <see cref="Rules.ServerLock"/> registers it with the mod's Charter and the server's
    /// value holds while it binds its players. Off, the stored choices are ignored and the altar rolls everything.
    /// </summary>
    public static class AltarSettings
    {
        public const string Section = "14 - Boss Altars";

        public static ConfigEntry<bool> Choice = null!;

        /// <summary>True when players may choose at the altar; the server's value while it binds.</summary>
        public static bool Enabled => Choice != null && Choice.Value;

        public static void Bind(ConfigFile config)
        {
            Choice = config.Bind(Section, "Altar Choice", true,
                "Interact with a boss altar (E, or Shift+E where E makes the offering) to choose the boss's stars and "
                + "aspect from what the rule file lets that boss roll. Either can stay on Random, which rolls at every "
                + "shift as before; a choice stays on the altar for every player until someone changes it. The fight pays "
                + "for the stars and aspect it has, chosen or rolled. With false the altar rolls everything and nobody "
                + "can choose. Set on the server: while it binds its players ('Lock to server' in 1 - General), the "
                + "server's value holds for everyone.");
        }
    }
}
