using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Stacks
{
    /// <summary>Section "4. Stacks", all synced. Values are read at use time.</summary>
    public static class StacksSettings
    {
        public const string Section = "4. Stacks";
        public const string StackSection = "4a. Item Stacks";
        public const string WeightSection = "4b. Item Weights";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<float> StackMultiplier { get; private set; }
        public static ConfigEntry<float> WeightMultiplier { get; private set; }
        public static ConfigEntry<bool> IgnoreTeleportRestriction { get; private set; }
        public static ConfigEntry<bool> MergeIntoChests { get; private set; }
        public static ConfigEntry<bool> PerItemConfigEntries { get; private set; }
        public static ConfigEntry<bool> WriteDocumentation { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, "Enabled", true,
                "Master switch of the Stacks module. Off: every item keeps its vanilla stack and weight, portals and chest merging are vanilla.");
            StackMultiplier = synced.Bind(Section, "Stack Multiplier", 1f,
                "Every stackable item's maximum stack is multiplied by this (rounded, at least 1). Items with a vanilla stack of 1 stay 1. OpenKeep.Stacks.yml may replace it.",
                acceptableValues: new AcceptableValueRange<float>(0.01f, 1000f));
            WeightMultiplier = synced.Bind(Section, "Weight Multiplier", 1f,
                "Every item's weight is multiplied by this. OpenKeep.Stacks.yml may replace it.",
                acceptableValues: new AcceptableValueRange<float>(0f, 100f));
            IgnoreTeleportRestriction = synced.Bind(Section, "Ignore Teleport Restriction", false,
                "The portal check accepts every item.");
            MergeIntoChests = synced.Bind(Section, "Merge Into Chests", true,
                "A stack dragged into an open container fills that container's partial stacks of the same item first; the remainder goes to the slot it was dropped on.");
            PerItemConfigEntries = synced.Bind(Section, "Per Item Config Entries", false,
                "Generates one <prefab>.Stack and <prefab>.Weight entry per item into sections 4a and 4b when the item database loads, for Configuration Manager users. 0 or the vanilla value means not set. OpenKeep.Stacks.yml wins where it names the same item.");
            WriteDocumentation = synced.Bind(Section, "Write Documentation", true,
                "On load, OpenKeep.Items.txt next to the .cfg lists every item with prefab name, display name, type, vanilla and current stack and weight; OpenKeep.Containers.txt lists every container prefab with its vanilla size. The console command 'openkeep write docs' writes them at any time.");
        }
    }
}
