using BepInEx.Configuration;

namespace EliteCrafting.Config
{
    /// <summary>
    /// The mod's one Charter: the server binds every player's gameplay settings and both YAML families while
    /// <c>Lock Configuration</c> is on, and refuses a player without the mod or with another version (the mod is
    /// required on every peer: its stone prefabs must exist everywhere). Created first in plugin Awake; the settings and
    /// the rule families register on it; <c>Charter.Install</c> runs once after every registration.
    /// </summary>
    public static class ServerBinding
    {
        public const string Title = "EliteCrafting";

        public static Charter.Charter Charter { get; private set; } = null!;

        /// <summary>Server only: when on, every player uses the server's gameplay values and YAML.</summary>
        public static ConfigEntry<bool> LockConfiguration { get; private set; } = null!;

        internal static void Create(ConfigFile config, string guid, string version)
        {
            Charter = new Charter.Charter(guid, Title, version, oldestAccepted: null, mandatory: true);
            LockConfiguration = config.Bind(ModSettings.General, "Lock Configuration", true,
                "Server only. When on, every player uses the server's gameplay settings and YAML files and cannot override them locally.");
            Charter.Binding(LockConfiguration);
        }
    }
}
