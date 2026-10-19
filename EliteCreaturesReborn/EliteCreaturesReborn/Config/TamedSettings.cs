using BepInEx.Configuration;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// What tamed creatures keep of this mod: its stars and its mutations, each on or off (the user, 2026-10-19). Off,
    /// the creature's owner takes them away the moment it is tamed (<see cref="Taming.TameSettle"/>) and every machine
    /// loads it again without them (<see cref="Taming.TameWatch"/>); the rolled stars and mutations stay in its ZDO, so
    /// turning a setting back on gives them back. They change the game, so like the horde's settings
    /// <see cref="Rules.ServerLock"/> registers them with the mod's Charter and the server's values hold while it binds.
    /// </summary>
    public static class TamedSettings
    {
        public const string Section = "15 - Tamed";

        public static ConfigEntry<bool> Stars = null!;
        public static ConfigEntry<bool> Mutations = null!;

        /// <summary>True when tamed creatures keep this mod's stars; the server's value while it binds.</summary>
        public static bool StarsKept => Stars == null || Stars.Value;

        /// <summary>True when tamed creatures keep their mutations; the server's value while it binds.</summary>
        public static bool MutationsKept => Mutations == null || Mutations.Value;

        public static void Bind(ConfigFile config)
        {
            Stars = config.Bind(Section, "Stars", true,
                "Tamed creatures keep this mod's stars. With false a tamed creature has the game's own stars only: the "
                + "level the game gave it before this mod's roll (one that appeared before this setting existed has none "
                + "kept and counts as plain), with the game's health, damage, look and loot for it, and its young take their parent's "
                + "level the game's way. Its stars from this mod come back when this is true again. Set on the server: "
                + "while it binds its players (the rule file's 'lock to server'), the server's value holds for everyone.");
            Mutations = config.Bind(Section, "Mutations", true,
                "Tamed creatures keep their mutations. With false a tamed creature carries none (a Thieving one drops "
                + "what it stole) and its young inherit none. Its mutation comes back when this is true again; young "
                + "born meanwhile have none. Set on the server: while it binds its players (the rule file's 'lock to "
                + "server'), the server's value holds for everyone.");
        }
    }
}
