namespace EliteCreaturesReborn
{
    /// <summary>Identity constants for the plugin. One place, so the GUID is never retyped.</summary>
    internal static class PluginInfo
    {
        // The GUID is the plugin's permanent identity (config file name, log lines, dependency handle). Fixed by the
        // spec's Identity section and must never change again - altering it orphans everyone's settings.
        public const string Guid = "gglasgow.elitecreaturesreborn";
        public const string Name = "Elite Creatures Reborn";
        public const string PluginVersion = "3.0.0";
        public const string Author = "Gerald Glasgow";
        public const string Licence = "GNU General Public License v3.0";
    }
}
