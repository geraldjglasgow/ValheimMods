using System.Collections.Generic;
using EliteCreaturesPack.Core;
using EliteCreaturesPack.Custom.Build;
using SyncedConfig;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>
    /// The custom creature files: EliteCreaturesPack.Creatures.yml and any EliteCreaturesPack.Creatures*.yml in the config
    /// folder and its subfolders, read through YamlConfig as one set (<see cref="CreatureFile"/>), reloaded within five
    /// seconds of an edit and editable in game like the mod's other files. A new main file is written from the embedded
    /// <c>Custom/Ready/creatures.yml</c>. The set's own sync (Charter, while the server binds) keeps a bound player's copy
    /// current, but builds never use it: the server builds from its own files, and players from the files the server
    /// built from (<see cref="ServerDefinitions"/>), so a file edited on a running server cannot reach a late joiner
    /// before the server itself has rebuilt.
    /// </summary>
    internal static class CreatureFiles
    {
        public const string Pattern = "EliteCreaturesPack.Creatures*.yml";
        public const string SyncKey = "ecp.creatures";
        public const string Resource = "EliteCreaturesPack.Custom.Ready.creatures.yml";

        private static YamlFileSet? set;
        private static YamlFileHub? hub;

        /// <summary>The last model that read without file-level errors, or null before the files load.</summary>
        public static CreatureFile? Current => set?.Current as CreatureFile;

        /// <summary>The files of <see cref="Current"/>, by full path, the main file first.</summary>
        public static IReadOnlyDictionary<string, string> Files => set?.Files ?? new Dictionary<string, string>();

        public static void Register(SyncedConfiguration synced)
        {
            hub = synced.Yaml;
            set = synced.AddYaml(new YamlFileSet(Pattern, SyncKey, () => new CreatureFile(), Applied)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(CreatureFiles).Assembly, Resource),
                SearchSubfolders = true,
                EditorLabel = () => "Edit custom creatures",
            });
        }

        /// <summary>Reads files that came from the server, logging their problems as the server did; null when unreadable.</summary>
        public static CreatureFile? Parse(IReadOnlyDictionary<string, string> files)
        {
            if (set == null || hub == null)
            {
                return null;
            }
            return hub.TryBuild(set, files, out YamlModel model, "from the server") ? model as CreatureFile : null;
        }

        private static void Applied(YamlModel model) =>
            SafeCall.Run("custom creature files applied", static file => BuildTiming.FilesApplied(file), (CreatureFile)model);
    }
}
