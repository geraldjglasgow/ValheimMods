namespace Broadside
{
    /// <summary>The words every sea edit shares (each kind keeps its own in its files).</summary>
    public static class KwSeaWords
    {
        public const string Nothing = "Nothing this tool changes there on a launched ship";
        public const string Unchanged = "Nothing changed";

        /// <summary>The tool panel's line for the tools that work at sea.</summary>
        public const string Panel = "the same on a launched ship where it floats, a workbench in range; its cost or refund is yours (hammer's remove: that part only)";

        /// <summary>A tool no sea kind serves, aimed at a launched ship.</summary>
        public static string DockOnly(string tool) => $"The {KwToolWords.Name(tool).ToLowerInvariant()} works only on the dry dock";

        /// <summary>The hammer's remove near a part but not on it.</summary>
        public static string NearMiss(string what) => $"Aim right at the {what} to remove it (the hull under it breaks the whole ship up)";
    }
}
