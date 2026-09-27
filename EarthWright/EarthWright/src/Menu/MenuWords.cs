using EarthWright.Core;

namespace EarthWright.Menu
{
    /// <summary>
    /// The Menu module's English words: every entry's name and description, EarthWright's descriptions of the game's
    /// terrain entries, the key hint words and the custom entry messages. Translations override them by key.
    /// </summary>
    public static class MenuWords
    {
        public const string CustomAdminOnly = "$ew_menu_custom_admin";
        public const string CustomNoConsole = "$ew_menu_custom_noconsole";
        public const string EditEntries = "$ew_menu_yaml_edit";
        public const string LevelLocked = "$ew_menu_level_locked";

        public static void Register()
        {
            foreach (EntryDef def in EntryDefs.All)
            {
                Language.Add(def.NameKey, def.EnglishName);
                Language.Add(def.DescriptionKey, def.EnglishDescription);
            }
            foreach (GameEntry entry in GameEntries.All)
            {
                Language.Add(entry.DescriptionKey, entry.EnglishDescription);
                if (entry.EnglishFlatDescription != null)
                    Language.Add(entry.FlatDescriptionKey, entry.EnglishFlatDescription);
            }
            RegisterHints();
            Language.Add("ew_menu_custom_admin", "Only an admin can use this entry.");
            Language.Add("ew_menu_custom_noconsole", "EarthWright: the console is not ready, the entry's command did not run.");
            Language.Add("ew_menu_yaml_edit", "Edit custom menu entries");
            Language.Add("ew_menu_level_locked", "Your tool's level does not allow this entry yet.");
        }

        private static void RegisterHints()
        {
            Language.Add("ew_menu_keys", "Keys:");
            Language.Add("ew_menu_key_wheel", "Wheel");
            Language.Add("ew_menu_hint_adjust", "size or value");
            Language.Add("ew_menu_hint_size", "size");
            Language.Add("ew_menu_hint_width", "width");
            Language.Add("ew_menu_hint_next", "pick the value");
            Language.Add("ew_menu_hint_shape", "shape");
            Language.Add("ew_menu_hint_rotate", "rotate");
            Language.Add("ew_menu_hint_style", "level style");
            Language.Add("ew_menu_hint_lock", "lock the height");
            Language.Add("ew_menu_hint_mode", "target mode");
            Language.Add("ew_menu_hint_hard", "hard level");
            Language.Add("ew_menu_hint_paint", "paint");
            Language.Add("ew_menu_hint_grid", "grid mode");
            Language.Add("ew_menu_hint_undo", "undo");
            Language.Add("ew_menu_hint_reset", "reset the brush area");
            Language.Add("ew_menu_hint_profile", "ramp profile");
            Language.Add("ew_menu_hint_back", "remove the last point");
            Language.Add("ew_menu_hint_quickramp", "ramp from your feet");
            Language.Add("ew_menu_hint_carve", "carve");
            Language.Add("ew_menu_hint_carvepaved", "carve paved");
        }
    }
}
