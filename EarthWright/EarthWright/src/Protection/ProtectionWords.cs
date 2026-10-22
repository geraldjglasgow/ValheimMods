using Translations;

namespace EarthWright.Protection
{
    /// <summary>
    /// The Protection module's words (English in translations/English.yml). Every refusal reason is a $token, so it is
    /// translated on the machine that shows it: owner refusals travel back to the sender as the token and are localized
    /// there.
    /// </summary>
    public static class ProtectionWords
    {
        public static string Ward { get; } = Lang.Token("ew_protect_ward");
        public static string NoBuild { get; } = Lang.Token("ew_protect_nobuild");
        public static string Dungeon { get; } = Lang.Token("ew_protect_dungeon");
        public static string Locked { get; } = Lang.Token("ew_protect_locked");
        public static string AdminsOnly { get; } = Lang.Token("ew_protect_adminsonly");
        public static string ToolsOff { get; } = Lang.Token("ew_protect_toolsoff");
        public static string AdminEntry { get; } = Lang.Token("ew_protect_adminentry");
        public static string ZoneOutside { get; } = Lang.Token("ew_protect_zone_outside");
        public static string ZoneInside { get; } = Lang.Token("ew_protect_zone_inside");
        public static string Combat { get; } = Lang.Token("ew_protect_combat");
        public static string OverrideHud { get; } = "<color=orange>" + Lang.Token("ew_protect_override_hud") + "</color>";
    }

    /// <summary>
    /// Words of the zone command and the server's replies. A reply naming a zone is a <see cref="LangWire"/> message, read
    /// in the requesting admin's language.
    /// </summary>
    public static class ZoneWords
    {
        public static string Full { get; } = Lang.Token("ew_protect_zone_full");
        public static string BadName { get; } = Lang.Token("ew_protect_zone_badname");
        public static string NotAdmin { get; } = Lang.Token("ew_protect_zone_notadmin");
        public static string Sent { get; } = Lang.Token("ew_protect_zone_sent");
        public static string NoZones { get; } = Lang.Token("ew_protect_zone_none");
        public static string Mode { get; } = Lang.Token("ew_protect_zone_mode");
        public static string Usage { get; } = Lang.Token("ew_protect_zone_usage");
        public static string NoPlayer { get; } = Lang.Token("ew_protect_zone_noplayer");
        public static string NoNetwork { get; } = Lang.Token("ew_protect_zone_nonetwork");

        public static string Added(string zone) => LangWire.Message("ew_protect_zone_added", zone);

        public static string Replaced(string zone) => LangWire.Message("ew_protect_zone_replaced", zone);

        public static string Removed(string name) => LangWire.Message("ew_protect_zone_removed", name);

        public static string Unknown(string name) => LangWire.Message("ew_protect_zone_unknown", name);
    }

    /// <summary>Words of the admin panel section.</summary>
    public static class PanelWords
    {
        public static string Title { get; } = Lang.Token("ew_protect_panel_title");
        public static string Lock { get; } = Lang.Token("ew_protect_panel_lock");
        public static string On { get; } = Lang.Token("ew_protect_panel_on");
        public static string Off { get; } = Lang.Token("ew_protect_panel_off");
        public static string LockButton { get; } = Lang.Token("ew_protect_panel_lockbutton");
        public static string UnlockButton { get; } = Lang.Token("ew_protect_panel_unlockbutton");
        public static string Tools { get; } = Lang.Token("ew_protect_panel_tools");
        public static string Zones { get; } = Lang.Token("ew_protect_panel_zones");
        public static string Remove { get; } = Lang.Token("ew_protect_panel_remove");
        public static string AddHere { get; } = Lang.Token("ew_protect_panel_addhere");
        public static string Name { get; } = Lang.Token("ew_protect_panel_name");
        public static string Radius { get; } = Lang.Token("ew_protect_panel_radius");
        public static string Player { get; } = Lang.Token("ew_protect_panel_player");
        public static string Next { get; } = Lang.Token("ew_protect_panel_next");
    }
}
