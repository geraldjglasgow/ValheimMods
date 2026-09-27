using System.Collections.Generic;
using System.IO;
using EarthWright.Core;
using HarmonyLib;
using SyncedConfig;
using YamlConfig;

namespace EarthWright.Protection
{
    /// <summary>
    /// The admin zones in effect on this machine.
    /// <list type="bullet">
    /// <item>The server reads them from EarthWright.Zones.yml, a YAML file set: found next to the cfg, hot reloaded,
    /// editable in the YAML editor and written back when admins change zones (<see cref="ZoneServer"/>).</item>
    /// <item>The server publishes them in a standing Charter article, which reaches every player whether or not the
    /// server binds its configuration. Every machine judges edits by that article only - players never by their own
    /// file - so the sender's check and the owner's check use the server's zones.</item>
    /// </list>
    /// </summary>
    public static class ZoneBook
    {
        public const string FileName = "EarthWright.Zones.yml";
        private const string ArticleName = "ew.zones";
        private const string FileSyncKey = "ew.zones.file";

        private static Charter.Article<List<string>> article;
        private static List<AdminZone> fileZones = new List<AdminZone>();
        private static List<string> decodedFrom;
        private static List<AdminZone> decoded = new List<AdminZone>();

        /// <summary>The zone file set (the server edits it through the YAML hub).</summary>
        public static YamlFileSet Set { get; private set; }

        /// <summary>The zones of this machine's own file: the truth on the server, unused on players.</summary>
        public static IReadOnlyList<AdminZone> FileZones => fileZones;

        public static void Initialize(SyncedConfiguration synced)
        {
            article = new Charter.Article<List<string>>(synced.Sync, ArticleName, new List<string>(), standing: true);
            Set = synced.AddYaml(new YamlFileSet(FileName, FileSyncKey, () => new ZoneFile(), model => Apply((ZoneFile)model))
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(ZoneBook).Assembly, "EarthWright.config." + FileName),
                EditorLabel = () => "Edit admin zones",
            });
        }

        /// <summary>The zones every check uses: the server's, as published. Decoded again only when the article changes.</summary>
        public static IReadOnlyList<AdminZone> Current
        {
            get
            {
                List<string> value = article?.Value;
                if (!ReferenceEquals(value, decodedFrom))
                {
                    decoded = ZoneWire.Decode(value);
                    decodedFrom = value;
                }
                return decoded;
            }
        }

        /// <summary>A parsed zone file (startup, reload, editor, admin change): kept, and published when this is the server.</summary>
        public static void Apply(ZoneFile file)
        {
            if (file == null)
                return;
            fileZones = new List<AdminZone>(file.Zones);
            Publish();
        }

        /// <summary>
        /// Server (dedicated, host or single player): pushes the file's zones to every player and to this machine's own
        /// table. The YAML hub applies files once at the main menu, before a world exists; the server publishes when it
        /// applies them again at network start (dedicated) or at the first spawn (host).
        /// </summary>
        public static void Publish()
        {
            if (article != null && Side.IsServer)
                article.Assign(ZoneWire.Encode(fileZones));
        }

        /// <summary>
        /// Host or single player, when a world starts: reads this machine's own zone file again. A player who was bound
        /// to another server earlier in this game session still holds that server's zone file in the YAML hub, and the
        /// hub would otherwise apply (and on the next change write back) those zones as this host's own.
        /// </summary>
        public static void ReloadLocalOnHost()
        {
            if (!Side.IsServer || Side.IsDedicated || Set == null || Plugin.Synced == null)
                return;
            Dictionary<string, string> files = ReadLocalFiles();
            if (files.Count > 0)
                Plugin.Synced.Yaml.Replace(Set, files, saveToDisk: false);
        }

        private static Dictionary<string, string> ReadLocalFiles()
        {
            Dictionary<string, string> files = new Dictionary<string, string>();
            foreach (string folder in Plugin.Synced.SearchPaths)
            {
                string path = Path.Combine(folder, FileName);
                try
                {
                    if (File.Exists(path))
                        files[path] = File.ReadAllText(path);
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogWarning($"Reading {path} failed: {e.Message}");
                }
            }
            return files;
        }
    }

    /// <summary>Re-reads the host's own zone file at every world start (see <see cref="ZoneBook.ReloadLocalOnHost"/>).</summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    public static class ZoneHostReloadPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => Safe.Run("EarthWright zone reload", ZoneBook.ReloadLocalOnHost);
    }
}
