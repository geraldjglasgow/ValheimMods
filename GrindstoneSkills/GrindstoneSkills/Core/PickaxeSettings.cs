using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using SyncedConfig;

namespace GrindstoneSkills
{
    /// <summary>
    /// Section 14: the Pickaxes module's master switch, each player's callout preference and the list of plain stone
    /// items, and the order the Pickaxes feature settings are bound in. The features share five sections: Pickaxes
    /// (this one and experience), Seams (clean strikes, Unbroken), Veins (Rich veins, Read the rock, Echo), Pickaxe Perks
    /// (extra ore, wear, splash) and Mine Finds. Gameplay values are synced and lockable; callouts are each player's own.
    /// </summary>
    public static class PickaxeSettings
    {
        public const string Section = "14 - Pickaxes";
        public const string SeamsSection = "15 - Seams";
        public const string VeinsSection = "16 - Veins";
        public const string PerksSection = "17 - Pickaxe Perks";
        public const string FindsSection = "18 - Mine Finds";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<bool> ShowCallouts { get; private set; }
        public static ConfigEntry<string> PlainStoneItems { get; private set; }

        public static void Initialize(SyncedConfiguration config)
        {
            Enabled = config.Bind(Section, "Pickaxes Enabled", true,
                "Turns every Pickaxes feature on or off: seams, veins, splash, perks, finds, experience changes. Off, rocks and ore deposits behave exactly as in the game. Levels are kept either way.");
            ShowCallouts = config.Bind(Section, "Show Callouts", true,
                "Shows words like Clean strike!, the Echo's distance and finds floating above rocks near you.", synced: false);
            PlainStoneItems = config.Bind(Section, "Plain Stone Items", "Stone, Grausten",
                "Item prefab names, comma-separated, that count as plain stone. A rock whose drops hold anything else is an ore deposit: it gets vein stars, extra ore, clean strike drops, more experience and Echo pings.");
            PickaxeExperienceSettings.Initialize(config);
            SeamSettings.Initialize(config);
            VeinSettings.Initialize(config);
            PickaxePerkSettings.Initialize(config);
            MineFindSettings.Initialize(config);
        }

        /// <summary>The Plain Stone Items setting as a set of prefab names, case-insensitive, blanks dropped.</summary>
        public static HashSet<string> ParsePlainStone(string value)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string part in (value ?? "").Split(','))
            {
                string name = part.Trim();
                if (name.Length > 0)
                    names.Add(name);
            }
            return names;
        }
    }
}
