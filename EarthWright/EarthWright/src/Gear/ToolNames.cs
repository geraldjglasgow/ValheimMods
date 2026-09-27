namespace EarthWright.Gear
{
    /// <summary>
    /// Item prefab names of the three terrain tools the Gear module levels. The hoe and the cultivator are the game's own;
    /// the shovel is EarthWright's, and its name is also its network prefab name, so it must never change.
    /// </summary>
    public static class ToolNames
    {
        public const string Hoe = "Hoe";
        public const string Cultivator = "Cultivator";
        public const string Shovel = "EW_Shovel";

        public static readonly string[] All = { Hoe, Cultivator, Shovel };
    }
}
