using SyncedConfig;
using YamlConfig;

namespace PackPanel.Backpacks
{
    /// <summary>
    /// PackPanel.Backpacks*.yml: what each backpack gives and costs. Synced from the server (Charter article
    /// <c>packpanel_backpacks</c>), hot reloaded and editable in game like the other YAML files. Applying it sets every
    /// kind's stats (the file over the built-in defaults), makes the recipes and the words follow, and the next frame the
    /// worn pack's slots (<see cref="BackpackWear"/>).
    /// </summary>
    public static class BackpacksFile
    {
        public const string DefaultResource = "PackPanel.config.PackPanel.Backpacks.yml";

        public static YamlFileSet Set { get; private set; }

        public static void Initialize(SyncedConfiguration synced)
        {
            Set = synced.AddYaml(new YamlFileSet("PackPanel.Backpacks*.yml", "packpanel_backpacks", () => new BackpacksModel(), Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(BackpacksFile).Assembly, DefaultResource),
                EditorLabel = () => "Edit backpacks",
            });
        }

        private static void Apply(YamlModel model)
        {
            BackpacksModel read = (BackpacksModel)model;
            foreach (BackpackKind kind in BackpackCatalog.All)
                kind.Stats = read.Stats.TryGetValue(kind.Id, out BackpackStats stats) ? stats : kind.Defaults;
            Crafting.CraftRecipes.RefreshAll();
            BackpackWords.DescribeAll();
            Plugin.Log.LogInfo($"PackPanel: backpacks applied, {read.Stats.Count} set in the file.");
        }
    }
}
