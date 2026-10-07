using OpenKeep.Core;
using PatchGuard;
using SyncedConfig;

namespace OpenKeep.Mimir
{
    /// <summary>
    /// Entry point of Mímir's Chest (section 15; decisions in CLAUDE.md, "Mímir's Chest"): binds the switch, registers the words, and puts the chest in
    /// or out of the hammer when the switch changes (a server's value arriving included).
    /// </summary>
    public static class MimirModule
    {
        public static void Initialize(SyncedConfiguration synced)
        {
            MimirSettings.Initialize(synced);
            Language.Add("ok_mimir_name", "Mímir's Chest");
            Language.Add("ok_mimir_desc", "A chest that never fills: like Mímir's well it always has room for more. Search it from the field above its grid.");
            Language.Add("ok_mimir_search", "Search the chest");
            Language.Add("ok_mimir_search_tip", "Type part of an item's name: everything else in the chest dims. A leading - leaves a word out. Escape clears it.");
            Language.Add("ok_mimir_filter_food", "Food");
            Language.Add("ok_mimir_filter_meads", "Meads");
            Language.Add("ok_mimir_filter_equipment", "Equipment");
            Language.Add("ok_mimir_filter_wood", "Wood");
            Language.Add("ok_mimir_filter_metal", "Ores and metals");
            Language.Add("ok_mimir_filter_trophies", "Trophies");
            Language.Add("ok_mimir_filter_other", "Other materials");
            Language.Add("ok_mimir_filter_starred", "Starred only");
            Language.Add("ok_mimir_sort_name", "Name");
            Language.Add("ok_mimir_sort_type", "Type");
            Language.Add("ok_mimir_sort_stars", "Stars");
            MimirSettings.Enabled.SettingChanged += (_, _) => Guard.Run("mimir chest switch", MimirHammer.Refresh);
        }
    }
}
