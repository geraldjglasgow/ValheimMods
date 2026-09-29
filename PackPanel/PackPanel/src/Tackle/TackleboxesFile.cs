using PackPanel.Crafting;
using SyncedConfig;
using YamlConfig;

namespace PackPanel.Tackle
{
    /// <summary>
    /// PackPanel.Tackleboxes*.yml: what each tacklebox gives and costs. Synced from the server (Charter article
    /// <c>packpanel_tackleboxes</c>), hot reloaded and editable in game like the other YAML files. Applying it sets every
    /// kind's stats (the file over the built-in defaults), makes the recipes and the words follow, and the next frame the
    /// box's cells (<see cref="TackleboxWear"/>).
    /// </summary>
    public static class TackleboxesFile
    {
        public const string DefaultResource = "PackPanel.config.PackPanel.Tackleboxes.yml";

        public static YamlFileSet Set { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            Set = synced.AddYaml(new YamlFileSet("PackPanel.Tackleboxes*.yml", "packpanel_tackleboxes", () => new TackleboxesModel(), Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(TackleboxesFile).Assembly, DefaultResource),
                EditorLabel = () => "Edit tackleboxes",
            });
        }

        private static void Apply(YamlModel model)
        {
            TackleboxesModel read = (TackleboxesModel)model;
            foreach (TackleboxKind kind in TackleboxCatalog.All)
                kind.Stats = read.Stats.TryGetValue(kind.Id, out TackleboxStats stats) ? stats : kind.Defaults;
            CraftRecipes.RefreshAll();
            TackleboxWords.DescribeAll();
            Plugin.Log.LogInfo($"PackPanel: tackleboxes applied, {read.Stats.Count} set in the file.");
        }
    }
}
