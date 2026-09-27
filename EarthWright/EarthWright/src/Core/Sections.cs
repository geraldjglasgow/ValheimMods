namespace EarthWright.Core
{
    /// <summary>
    /// The .cfg section names, numbered so the file reads in the order of the README. Each module binds its keys under
    /// its own section; a module never binds into another module's section.
    /// </summary>
    public static class Sections
    {
        public const string General = "0. General";
        public const string Brush = "1. Brush";
        public const string Target = "2. Target Height";
        public const string Operations = "3. Operations";
        public const string Paths = "4. Ramps and Roads";
        public const string Reset = "5. Reset and Clearing";
        public const string Limits = "6. Height Limits";
        public const string History = "7. Undo";
        public const string Costs = "8. Costs";
        public const string Preview = "9. Preview and HUD";
        public const string Controls = "10. Controls";
        public const string Protection = "11. Protection";
        public const string Menu = "12. Menu";
        public const string Gear = "13. Tools";
        public const string Cultivator = "14. Cultivator";
        public const string Roads = "15. Road Travel";
    }
}
