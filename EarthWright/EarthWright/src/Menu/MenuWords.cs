namespace EarthWright.Menu
{
    /// <summary>
    /// The Menu module's message words as $tokens. Every entry's name and description, EarthWright's descriptions of the
    /// game's terrain entries and the key hint words are in translations/English.yml too, under "ew_menu_..." keys.
    /// </summary>
    public static class MenuWords
    {
        public const string CustomAdminOnly = "$ew_menu_custom_admin";
        public const string CustomNoConsole = "$ew_menu_custom_noconsole";
        public const string EditEntries = "$ew_menu_yaml_edit";
        public const string LevelLocked = "$ew_menu_level_locked";
    }
}
