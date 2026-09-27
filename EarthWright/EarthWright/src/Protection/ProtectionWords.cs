using EarthWright.Core;

namespace EarthWright.Protection
{
    /// <summary>
    /// The Protection module's words. Every refusal reason is a $token, so it is translated on the machine that shows
    /// it: owner refusals travel back to the sender as the token and are localized there.
    /// </summary>
    public static class ProtectionWords
    {
        public static string Ward { get; private set; }
        public static string NoBuild { get; private set; }
        public static string Dungeon { get; private set; }
        public static string Locked { get; private set; }
        public static string AdminsOnly { get; private set; }
        public static string ToolsOff { get; private set; }
        public static string AdminEntry { get; private set; }
        public static string ZoneOutside { get; private set; }
        public static string ZoneInside { get; private set; }
        public static string Combat { get; private set; }
        public static string OverrideHud { get; private set; }

        public static void Register()
        {
            RegisterRefusals();
            ZoneWords.Register();
            PanelWords.Register();
        }

        private static void RegisterRefusals()
        {
            Ward = Language.Add("ew_protect_ward", "A ward you are not permitted on protects this ground");
            NoBuild = Language.Add("ew_protect_nobuild", "The ground cannot be changed in this place");
            Dungeon = Language.Add("ew_protect_dungeon", "The ground cannot be changed from inside a dungeon");
            Locked = Language.Add("ew_protect_locked", "Terrain editing is locked on this server");
            AdminsOnly = Language.Add("ew_protect_adminsonly", "Only admins may change terrain on this server");
            ToolsOff = Language.Add("ew_protect_toolsoff", "Terrain tools are switched off on this server");
            AdminEntry = Language.Add("ew_protect_adminentry", "Only an admin can use this entry");
            ZoneOutside = Language.Add("ew_protect_zone_outside", "Terrain can only be changed inside an admin zone");
            ZoneInside = Language.Add("ew_protect_zone_inside", "An admin zone protects this ground");
            Combat = Language.Add("ew_protect_combat", "Enemies are hunting you; terrain tools wait until the fight is over");
            OverrideHud = Language.Add("ew_protect_override_hud", "<color=orange>Admin override: height limits lifted</color>");
        }
    }

    /// <summary>Words of the zone command and the server's replies.</summary>
    public static class ZoneWords
    {
        public static string Added { get; private set; }
        public static string Replaced { get; private set; }
        public static string Removed { get; private set; }
        public static string Unknown { get; private set; }
        public static string Full { get; private set; }
        public static string BadName { get; private set; }
        public static string NotAdmin { get; private set; }
        public static string Sent { get; private set; }
        public static string NoZones { get; private set; }
        public static string Mode { get; private set; }
        public static string Usage { get; private set; }
        public static string NoPlayer { get; private set; }
        public static string NoNetwork { get; private set; }

        public static void Register()
        {
            Added = Language.Add("ew_protect_zone_added", "Admin zone added:");
            Replaced = Language.Add("ew_protect_zone_replaced", "Admin zone replaced:");
            Removed = Language.Add("ew_protect_zone_removed", "Admin zone removed:");
            Unknown = Language.Add("ew_protect_zone_unknown", "There is no admin zone named");
            Full = Language.Add("ew_protect_zone_full", "There are already 100 admin zones; remove one first");
            BadName = Language.Add("ew_protect_zone_badname", "A zone name uses letters, digits, '-' and '_' only (at most 32)");
            NotAdmin = Language.Add("ew_protect_zone_notadmin", "Only an admin can change the admin zones");
            Sent = Language.Add("ew_protect_zone_sent", "Sent to the server...");
            NoZones = Language.Add("ew_protect_zone_none", "There are no admin zones.");
            Mode = Language.Add("ew_protect_zone_mode", "Admin zone mode");
            Usage = Language.Add("ew_protect_zone_usage", "Usage: ew zone add <name> <radius 5-200> [player] | ew zone remove <name> | ew zone list");
            NoPlayer = Language.Add("ew_protect_zone_noplayer", "A zone is placed at your character; join a world first");
            NoNetwork = Language.Add("ew_protect_zone_nonetwork", "Not connected to a world");
        }
    }

    /// <summary>Words of the admin panel section.</summary>
    public static class PanelWords
    {
        public static string Title { get; private set; }
        public static string Lock { get; private set; }
        public static string On { get; private set; }
        public static string Off { get; private set; }
        public static string LockButton { get; private set; }
        public static string UnlockButton { get; private set; }
        public static string Tools { get; private set; }
        public static string Zones { get; private set; }
        public static string Remove { get; private set; }
        public static string AddHere { get; private set; }
        public static string Name { get; private set; }
        public static string Radius { get; private set; }
        public static string Player { get; private set; }
        public static string Next { get; private set; }

        public static void Register()
        {
            Title = Language.Add("ew_protect_panel_title", "Protection (admin)");
            Lock = Language.Add("ew_protect_panel_lock", "Terrain lock");
            On = Language.Add("ew_protect_panel_on", "on");
            Off = Language.Add("ew_protect_panel_off", "off");
            LockButton = Language.Add("ew_protect_panel_lockbutton", "Lock terrain");
            UnlockButton = Language.Add("ew_protect_panel_unlockbutton", "Unlock terrain");
            Tools = Language.Add("ew_protect_panel_tools", "Terrain tools allowed");
            Zones = Language.Add("ew_protect_panel_zones", "Admin zones");
            Remove = Language.Add("ew_protect_panel_remove", "Remove");
            AddHere = Language.Add("ew_protect_panel_addhere", "Add zone here");
            Name = Language.Add("ew_protect_panel_name", "Name");
            Radius = Language.Add("ew_protect_panel_radius", "Radius");
            Player = Language.Add("ew_protect_panel_player", "Player (optional)");
            Next = Language.Add("ew_protect_panel_next", "Change");
        }
    }
}
