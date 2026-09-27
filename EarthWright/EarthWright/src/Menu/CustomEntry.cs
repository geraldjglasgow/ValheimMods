using System.Globalization;
using EarthWright.Actions;
using EarthWright.Terrain;

namespace EarthWright.Menu
{
    /// <summary>Which tool's menu lists a custom entry (the YAML key <c>tool</c>).</summary>
    public enum CustomTool
    {
        Hoe,
        Cultivator,
    }

    /// <summary>
    /// One entry of EarthWright.Entries.yml: a build-menu entry that runs a console command. The piece is named
    /// <see cref="PrefabName"/> (stable, so favourites and the known-piece list survive edits of the texts) and its
    /// action's special key is "custom:&lt;id&gt;". Its name and description are the file's own texts, shown as written
    /// (they may contain the game's $words); they are the server admin's words, not part of EarthWright's translations.
    /// </summary>
    public sealed class CustomEntry
    {
        public string Id;
        public string Name;
        public string Description;
        public string Icon;
        public CustomTool Tool = CustomTool.Hoe;
        public int? Position;
        public float? Radius;
        public float? Height;
        public BrushShape? Shape;
        public string Command;
        public bool Repeat;
        public bool Admin;

        public string PrefabName => "ew_custom_" + Id;

        public string SpecialKey => "custom:" + Id;

        public ToolFamily Family => Tool == CustomTool.Cultivator ? ToolFamily.Cultivator : ToolFamily.Hoe;

        /// <summary>The game piece the entry's prefab is cloned from: a paint-only piece of its tool.</summary>
        public string BasePrefab => Tool == CustomTool.Cultivator ? "replant_v2" : "path_v2";

        /// <summary>Everything that shapes the entry, to tell whether a reloaded file changed it.</summary>
        public string Signature()
        {
            return string.Join("|", Id, Name, Description, Icon, Tool, Position, F(Radius), F(Height), Shape, Command, Repeat, Admin);
        }

        /// <summary>
        /// The action a click performs: the custom handler, with the entry's radius and height as the brush's starting
        /// size and amount (the Brush module reads them from the action like any entry's).
        /// </summary>
        public ToolAction CreateAction()
        {
            return new ToolAction
            {
                Id = PrefabName, Family = Family, Special = SpecialKey, AdminOnly = Admin,
                BaseRadius = Radius ?? 2f, Amount = Height ?? 1f, UsesAmount = Height.HasValue, Hardness = 1f,
            };
        }

        private static string F(float? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "";
    }
}
