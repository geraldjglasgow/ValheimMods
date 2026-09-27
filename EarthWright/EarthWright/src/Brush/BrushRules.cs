using EarthWright.Actions;
using EarthWright.Core;
using SyncedConfig;
using YamlConfig;

namespace EarthWright.Brush
{
    /// <summary>
    /// EarthWright.Brushes*.yml: per entry and per tool family starting sizes, radius limits, amounts, hardness, style,
    /// shape and whether the size keys resize it. Synced from the server like every EarthWright YAML file (players use
    /// the server's file while it binds them), hot reloaded; a reload forgets the values remembered this session so the
    /// new defaults show.
    /// </summary>
    public static class BrushRules
    {
        public const string Resource = "EarthWright.config.EarthWright.Brushes.yml";

        private static BrushRulesModel current;

        public static YamlFileSet Files { get; private set; }

        public static void Register(SyncedConfiguration synced)
        {
            Files = synced.AddYaml(new YamlFileSet("EarthWright.Brushes*.yml", "earthwright_brushes", () => new BrushRulesModel(), Apply)
            {
                DefaultContent = SyncedConfiguration.EmbeddedResource(typeof(BrushRules).Assembly, Resource),
                EditorLabel = () => "Edit brush sizes",
            });
        }

        /// <summary>Every machine that loads the files: the parsed rules replace the old ones.</summary>
        private static void Apply(YamlModel model)
        {
            current = model as BrushRulesModel;
            BrushMemory.Forget();
            if (GeneralSettings.DebugLog.Value && current != null)
                Plugin.Log.LogInfo($"EarthWright brushes: {current.EntryRules.Count} entry rules, {current.FamilyRules.Count} family rules");
        }

        public static EntryRule Entry(string pieceName)
        {
            if (current == null || pieceName == null)
                return EntryRule.Empty;
            return current.EntryRules.TryGetValue(pieceName, out EntryRule rule) ? rule : EntryRule.Empty;
        }

        public static EntryRule Family(ToolFamily family)
        {
            if (current == null)
                return EntryRule.Empty;
            return current.FamilyRules.TryGetValue(family, out EntryRule rule) ? rule : EntryRule.Empty;
        }
    }
}
