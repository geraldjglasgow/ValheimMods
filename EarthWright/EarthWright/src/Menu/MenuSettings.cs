using System.Collections.Generic;
using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;

namespace EarthWright.Menu
{
    /// <summary>
    /// Section "12. Menu": which entries the hoe and cultivator menus list (synced, so every player sees the
    /// server's menu), how the game's paved road and cultivate entries behave (synced), the repeat rate of custom
    /// entries (synced), and each player's own display choices: key hints in descriptions and the full build menu.
    /// </summary>
    public static class MenuSettings
    {
        private static readonly Dictionary<string, ConfigEntry<bool>> toggles = new Dictionary<string, ConfigEntry<bool>>();

        public static ConfigEntry<bool> PavedRoadLevels { get; private set; }
        public static ConfigEntry<bool> CultivateLevels { get; private set; }
        public static ConfigEntry<bool> TerraformIgnoresLimits { get; private set; }
        public static ConfigEntry<float> CustomRepeatInterval { get; private set; }
        public static ConfigEntry<bool> ShowKeyHints { get; private set; }
        public static ConfigEntry<bool> FullBuildMenu { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            foreach (GameEntry entry in GameEntries.All)
                toggles[entry.Id] = synced.Bind(Sections.Menu, "Enable " + entry.Label, true,
                    $"Lists the game's own '{entry.Label}' entry in the {Tool(entry.Tool)} menu. Off removes it for every player.");
            foreach (EntryDef def in EntryDefs.All)
                toggles[def.Id] = synced.Bind(Sections.Menu, "Enable " + def.Label, true, ToggleDescription(def));
            BindBehaviour(synced);
        }

        private static void BindBehaviour(SyncedConfiguration synced)
        {
            PavedRoadLevels = synced.Bind(Sections.Menu, "Paved Road Levels", true,
                "On: the game's Paved road entry levels the ground toward the target height and paves it, as in the unmodded game. Off: it only paves, the height stays as it is.");
            CultivateLevels = synced.Bind(Sections.Menu, "Cultivate Levels", true,
                "On: the cultivator's Cultivate entry evens out the ground as it tills, as in the unmodded game. Off: it only tills, the height stays as it is.");
            TerraformIgnoresLimits = synced.Bind(Sections.Menu, "Terraform Ignores Height Limits", true,
                "On: the admin-only Terraform entry levels past the raise and dig limits. Off: it obeys the height limits like every other entry.");
            CustomRepeatInterval = synced.Bind(Sections.Menu, "Custom Entry Repeat Interval", 0.25f,
                "Seconds between two runs of a custom entry's command while the button is held (entries with 'repeat: true' in EarthWright.Entries.yml).",
                acceptableValues: new AcceptableValueRange<float>(0.05f, 5f));
            ShowKeyHints = synced.Bind(Sections.Menu, "Show Key Hints In Descriptions", true,
                "Your own choice: the descriptions in the hoe and cultivator menus end with the keys that work with each entry, as you have bound them.",
                synced: false);
            FullBuildMenu = synced.Bind(Sections.Menu, "Full Build Menu", true,
                "Your own choice: the hoe and cultivator use the game's full build menu, like the hammer: a search field, recent pieces and favourites (middle click an entry). Off: the game's plain list without a search field.",
                synced: false);
        }

        /// <summary>The entry's toggle is on; true for entries without a toggle (custom entries).</summary>
        public static bool Toggle(string id) => !toggles.TryGetValue(id, out ConfigEntry<bool> entry) || entry.Value;

        private static string ToggleDescription(EntryDef def)
        {
            string text = $"Lists EarthWright's '{def.EnglishName}' entry in the {Tool(def.Tool)} menu. Off removes it for every player.";
            if (def.Id == "ew_clear")
                text += " The entry also needs clearing to be switched on in section 5.";
            if (def.Action.AdminOnly)
                text += " Only admins see it.";
            return text;
        }

        private static string Tool(Actions.ToolFamily tool)
        {
            return tool == Actions.ToolFamily.Cultivator ? "cultivator's" : "hoe's";
        }
    }
}
