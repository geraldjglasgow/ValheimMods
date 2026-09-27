using BepInEx.Configuration;
using EarthWright.Core;
using SyncedConfig;
using UnityEngine;

namespace EarthWright.Protection
{
    /// <summary>Who may use terrain tools at all: the server switch "Terrain Tools Allowed".</summary>
    public enum ToolAccess
    {
        Everyone = 0,
        AdminsOnly = 1,
        Nobody = 2,
    }

    /// <summary>What the admin zones mean.</summary>
    public enum ZoneMode
    {
        /// <summary>Admin zones do nothing.</summary>
        Off = 0,
        /// <summary>Terrain tools work only inside a zone that allows the player.</summary>
        OnlyInsideZones = 1,
        /// <summary>Terrain tools never work inside a zone, except for the player a zone names.</summary>
        NeverInsideZones = 2,
    }

    /// <summary>
    /// Section "11. Protection". Everything that decides who may change which ground is gameplay, so it is synced and
    /// lockable; only the admin limit-override key is each player's own. Values are read at use time, so a hot reload
    /// or a change pushed by the server applies at once.
    /// </summary>
    public static class ProtectionSettings
    {
        /// <summary>Ore deposits, mud piles (iron), silver veins, obsidian, meteorites, gold veins and buried treasure of this game build.</summary>
        public const string DefaultDigObjects =
            "rock4_copper, rock4_copper_frac, MineRock_Copper, MineRock_Tin, MineRock_Iron, mudpile, mudpile_beacon, mudpile_old, " +
            "mudpile_frac, mudpile2, mudpile2_frac, silvervein, silvervein_frac, rock3_silver, rock3_silver_frac, MineRock_Obsidian, " +
            "MineRock_Meteorite, goldvein, goldvein_frac, TreasureChest_meadows_buried, TreasureChest_memorial_buried, " +
            "Pickable_MountainRemains01_buried";

        public static ConfigEntry<bool> RespectWards { get; private set; }
        public static ConfigEntry<bool> RespectNoBuild { get; private set; }
        public static ConfigEntry<bool> RefuseInDungeons { get; private set; }
        public static ConfigEntry<ToolAccess> ToolsAllowed { get; private set; }
        public static ConfigEntry<bool> LockTerrain { get; private set; }
        public static ConfigEntry<bool> AdminsBypassLock { get; private set; }
        public static ConfigEntry<string> ExemptTools { get; private set; }
        public static ConfigEntry<ZoneMode> Zones { get; private set; }
        public static ConfigEntry<bool> AdminsBypassZones { get; private set; }
        public static ConfigEntry<bool> CombatLock { get; private set; }
        public static ConfigEntry<float> CombatRadius { get; private set; }
        public static ConfigEntry<KeyboardShortcut> OverrideKey { get; private set; }
        public static ConfigEntry<bool> OverrideNeedsGodMode { get; private set; }
        public static ConfigEntry<bool> StrictDigExceptions { get; private set; }
        public static ConfigEntry<string> DigObjects { get; private set; }
        public static ConfigEntry<float> DigRadius { get; private set; }
        public static ConfigEntry<bool> DigInTar { get; private set; }

        /// <summary>The exempt tools as a set, parsed once per value.</summary>
        public static NameList ExemptToolNames { get; private set; }

        /// <summary>The dig exception objects as a set, parsed once per value.</summary>
        public static NameList DigObjectNames { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            BindPlaces(synced);
            BindAccess(synced);
            BindZonesAndCombat(synced);
            BindOverride(synced);
            BindDigExceptions(synced);
            ExemptToolNames = new NameList(ExemptTools);
            DigObjectNames = new NameList(DigObjects);
        }

        private static void BindPlaces(SyncedConfiguration synced)
        {
            RespectWards = synced.Bind(Sections.Protection, "Respect Wards", true,
                "Terrain edits are refused when any part of the brush reaches into an active ward you are not permitted on. Checked on your machine and again by the machine that owns the ground. Off: only the game's own check of the aimed point applies (to clicks).");
            RespectNoBuild = synced.Bind(Sections.Protection, "Respect No-Build Zones", true,
                "Terrain edits are refused when the brush touches a place where the game forbids building, such as boss altars and the traders. Off: only the game's own check of the aimed point applies (to clicks).");
            RefuseInDungeons = synced.Bind(Sections.Protection, "Refuse In Dungeons", true,
                "Terrain edits are refused while you are inside a dungeon or another interior, so the ground above cannot be changed from below.");
        }

        private static void BindAccess(SyncedConfiguration synced)
        {
            ToolsAllowed = synced.Bind(Sections.Protection, "Terrain Tools Allowed", ToolAccess.Everyone,
                "Who may change terrain with the hoe, the cultivator and EarthWright's tools: Everyone, AdminsOnly (players on the server's admin list and the host) or Nobody.");
            LockTerrain = synced.Bind(Sections.Protection, "Lock Terrain Editing", false,
                "Locks all terrain editing: EarthWright's tools, the game's own hoe and cultivator entries and digging with the pickaxe. Admins (see below) and exempt tools still work.");
            AdminsBypassLock = synced.Bind(Sections.Protection, "Admins Bypass Lock", true,
                "Admins may still change terrain while terrain editing is locked. Their edits are checked by the server.");
            ExemptTools = synced.Bind(Sections.Protection, "Exempt Tools", "",
                "Item prefab names that keep working while terrain editing is locked, separated by commas, for example 'Cultivator' or 'Cultivator, PickaxeIron'. EarthWright's shovel is 'EW_Shovel' (the word 'Shovel' works too). An exempt tool may also clear objects and uproot while locked.");
        }

        private static void BindZonesAndCombat(SyncedConfiguration synced)
        {
            Zones = synced.Bind(Sections.Protection, "Admin Zone Mode", ZoneMode.Off,
                "Off: admin zones do nothing. OnlyInsideZones: terrain can only be changed inside an admin zone. NeverInsideZones: terrain can never be changed inside an admin zone. A zone that names a player is that player's: only they may edit inside it (OnlyInsideZones), or they still may (NeverInsideZones). Admins manage zones with 'ew zone'; the server keeps them in EarthWright.Zones.yml.");
            AdminsBypassZones = synced.Bind(Sections.Protection, "Admins Bypass Zones", true,
                "Admins may change terrain regardless of the admin zones. Their edits are checked by the server.");
            CombatLock = synced.Bind(Sections.Protection, "Combat Lock", false,
                "Blocks terrain tools while a hostile creature within the combat lock radius is alerted and hunting you.");
            CombatRadius = synced.Bind(Sections.Protection, "Combat Lock Radius", 30f,
                "How far away, in metres, an alerted hostile creature hunting you blocks your terrain tools (when Combat Lock is on).",
                acceptableValues: new AcceptableValueRange<float>(5f, 100f));
        }

        private static void BindOverride(SyncedConfiguration synced)
        {
            OverrideKey = synced.Bind(Sections.Protection, "Admin Limit Override Key", new KeyboardShortcut(KeyCode.RightAlt),
                "Admins only: hold this key while changing terrain to ignore the height limits. The server checks that you are an admin. Your own key; it does nothing for other players.",
                synced: false);
            OverrideNeedsGodMode = synced.Bind(Sections.Protection, "Limit Override Needs God Mode", false,
                "When on, the admin limit override key only works while the admin is in god mode.");
        }

        private static void BindDigExceptions(SyncedConfiguration synced)
        {
            StrictDigExceptions = synced.Bind(Sections.Protection, "Strict Dig Exceptions", false,
                "When on, the dig limit (section 6) does not apply near the objects listed below or where tar covers the ground, so strict dig limits still let players dig out ore and buried treasure.");
            DigObjects = synced.Bind(Sections.Protection, "Dig Exception Objects", DefaultDigObjects,
                "Prefab names of the objects near which the dig limit is lifted, separated by commas (ore deposits, mud piles, buried treasure).");
            DigRadius = synced.Bind(Sections.Protection, "Dig Exception Radius", 8f,
                "How far from such an object, in metres, the dig limit is lifted.",
                acceptableValues: new AcceptableValueRange<float>(1f, 32f));
            DigInTar = synced.Bind(Sections.Protection, "Dig Exception In Tar", true,
                "Lifts the dig limit where tar covers the ground (when Strict Dig Exceptions is on).");
        }
    }
}
