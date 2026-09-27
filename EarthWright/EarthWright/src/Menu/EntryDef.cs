using EarthWright.Actions;

namespace EarthWright.Menu
{
    /// <summary>
    /// One of EarthWright's own build-menu entries: the game piece it is cloned from, the tool whose menu lists it, its
    /// words and icon, the action a click performs and the keys its description names. The prefab name is <see cref="Id"/>.
    /// </summary>
    public sealed class EntryDef
    {
        /// <summary>The piece prefab name and the action id ("ew_lower").</summary>
        public string Id;

        /// <summary>The name in the entry's toggle setting, "Enable &lt;Label&gt;".</summary>
        public string Label;

        /// <summary>The game piece the entry is cloned from (placement flags, ghost, effects).</summary>
        public string BasePrefab;

        /// <summary>The embedded icon's name: EarthWright.assets.menu_&lt;Icon&gt;.png.</summary>
        public string Icon;

        public string EnglishName;

        public string EnglishDescription;

        public ToolAction Action;

        public HintKind[] Hints;

        /// <summary>Which tool lists the entry.</summary>
        public ToolFamily Tool => Action.Family;

        /// <summary>The id without the "ew_" prefix, used in word keys.</summary>
        public string Short => Id.StartsWith("ew_") ? Id.Substring(3) : Id;

        public string NameKey => "ew_menu_" + Short;

        public string DescriptionKey => "ew_menu_" + Short + "_desc";
    }
}
