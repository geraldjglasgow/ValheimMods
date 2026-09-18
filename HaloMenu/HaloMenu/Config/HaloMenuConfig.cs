using BepInEx.Configuration;

namespace HaloMenu.Config
{
    /// <summary>Mod-wide config: the Debug section, plus the default ring's own Input/Layout/Visual sections.</summary>
    public sealed class HaloMenuConfig
    {
        public ConfigEntry<HaloLogLevel> LogLevel;
        public RingSettings DefaultRing;

        public static HaloMenuConfig Bind(ConfigFile config)
        {
            HaloMenuConfig c = new HaloMenuConfig
            {
                LogLevel = config.Bind("Debug", "LogLevel", HaloLogLevel.Warning, "How chatty HaloMenu's own log lines are."),
                DefaultRing = RingSettings.Bind(config, "", RingDefaults.Default()),
            };
            return c;
        }
    }
}
