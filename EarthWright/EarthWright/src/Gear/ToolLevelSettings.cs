using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.Gear
{
    /// <summary>
    /// The level settings of one terrain tool (hoe or cultivator): its highest upgrade level, what each upgrade
    /// costs and how much durability each level adds. One instance per tool keeps the three sets of keys alike.
    /// </summary>
    public sealed class ToolLevelSettings
    {
        /// <summary>The item prefab name ("Hoe", "Cultivator").</summary>
        public string Prefab { get; }

        public ConfigEntry<int> MaxLevel { get; private set; }
        public ConfigEntry<string> UpgradeCost { get; private set; }
        public ConfigEntry<float> DurabilityPerLevel { get; private set; }

        private ToolLevelSettings(string prefab)
        {
            Prefab = prefab;
        }

        /// <summary>Binds "&lt;Tool&gt; Max Level", "&lt;Tool&gt; Upgrade Cost" and "&lt;Tool&gt; Durability Per Level".</summary>
        public static ToolLevelSettings Bind(SyncedConfiguration synced, string prefab, string label, string defaultCost, string costNote)
        {
            ToolLevelSettings tool = new ToolLevelSettings(prefab);
            tool.MaxLevel = synced.Bind(Sections.Gear, label + " Max Level", 6,
                "The highest upgrade level of the " + label.ToLowerInvariant() + ". Upgrades are made at its crafting station like any other tool's. " +
                "Tools already above a lowered maximum keep their level.",
                acceptableValues: new AcceptableValueRange<int>(1, 10));
            tool.UpgradeCost = synced.Bind(Sections.Gear, label + " Upgrade Cost", defaultCost,
                "What one upgrade of the " + label.ToLowerInvariant() + " costs, as Item:Amount pairs separated by commas (item prefab names such as Wood, Stone, Bronze). " +
                "The game multiplies these amounts for higher levels as it does for every tool. " + costNote);
            tool.DurabilityPerLevel = synced.Bind(Sections.Gear, label + " Durability Per Level", 200f,
                "Durability the " + label.ToLowerInvariant() + " gains with each upgrade level (the game's own value is 200).",
                acceptableValues: new AcceptableValueRange<float>(0f, 10000f));
            return tool;
        }
    }
}
