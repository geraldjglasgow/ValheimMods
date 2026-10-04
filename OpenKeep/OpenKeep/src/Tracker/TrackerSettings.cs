using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace OpenKeep.Tracker
{
    /// <summary>
    /// Section "13. Recipe Tracker": recipes pinned to the screen with their materials. It shows the player what they
    /// carry and changes nothing in the world, so every entry is the player's own (unsynced). Read at use time.
    /// </summary>
    public static class TrackerSettings
    {
        public const string Section = "13. Recipe Tracker";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<int> MaxTracked { get; private set; }
        public static ConfigEntry<bool> CountNearbyChests { get; private set; }
        public static ConfigEntry<bool> UntrackWhenCrafted { get; private set; }
        public static ConfigEntry<bool> HideInCombat { get; private set; }
        public static ConfigEntry<bool> HideWithMap { get; private set; }
        public static ConfigEntry<float> Scale { get; private set; }
        public static ConfigEntry<TrackerFont> Font { get; private set; }
        public static ConfigEntry<int> FontSize { get; private set; }
        public static ConfigEntry<string> HaveColour { get; private set; }
        public static ConfigEntry<string> MissingColour { get; private set; }
        public static ConfigEntry<string> ReadyColour { get; private set; }
        public static ConfigEntry<float> BackgroundOpacity { get; private set; }
        public static ConfigEntry<string> Position { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            Enabled = synced.Bind(Section, "Enabled", true,
                "Track a recipe (right click it in the crafting list, its Track button, or right stick down on a gamepad) to keep it on screen with every material it needs: what you have against what the tracked amount needs. With the inventory open, drag the tracker by its title, change an amount with - and + (Shift: tens) or the mouse wheel, and remove a recipe with X.", synced: false);
            MaxTracked = synced.Bind(Section, "Max Tracked", 6, "The most recipes tracked at once.", synced: false,
                acceptableValues: new AcceptableValueRange<int>(1, 12));
            CountNearbyChests = synced.Bind(Section, "Count Nearby Chests", true,
                "Materials in the containers Reach would craft from where you stand count too (only while Reach and its Crafting switch are on). Off: only what you carry.", synced: false);
            UntrackWhenCrafted = synced.Bind(Section, "Untrack When Crafted", true,
                "Crafting a tracked recipe counts its amount down; once that many are made it leaves the tracker. Off: it stays until you remove it.", synced: false);
            HideInCombat = synced.Bind(Section, "Hide In Combat", true,
                "Hidden while a creature is after you, and for 5 seconds after.", synced: false);
            HideWithMap = synced.Bind(Section, "Hide With Map", true, "Hidden while the large map is open.", synced: false);
            BindLook(synced);
        }

        private static void BindLook(SyncedConfiguration synced)
        {
            Scale = synced.Bind(Section, "Scale", 1f, "The tracker's size.", synced: false,
                acceptableValues: new AcceptableValueRange<float>(0.5f, 2f));
            Font = synced.Bind(Section, "Font", TrackerFont.Sans,
                "The game's typeface to use: Sans (its body text), Serif (its item names) or Norse (its titles).", synced: false);
            FontSize = synced.Bind(Section, "Font Size", 16, "The text size; the tracker widens with it.", synced: false,
                acceptableValues: new AcceptableValueRange<int>(10, 28));
            HaveColour = synced.Bind(Section, "Have Colour", "#FFFFFF", "A material you have enough of (HTML colour).", synced: false);
            MissingColour = synced.Bind(Section, "Missing Colour", "#FF6A5A", "A material you are short of (HTML colour).", synced: false);
            ReadyColour = synced.Bind(Section, "Ready Colour", "#FFB65C", "A recipe's name once every material is there (HTML colour).", synced: false);
            BackgroundOpacity = synced.Bind(Section, "Background Opacity", 0.56f, "The dark background behind the tracker, 0 (none) to 1.", synced: false,
                acceptableValues: new AcceptableValueRange<float>(0f, 1f));
            Position = synced.Bind(Section, "Position", "",
                "Where the tracker's top left corner sits: x right and y down from the screen's top left, in interface units (20,330 is the default place at the left edge); written when you drag it. A place off the screen is pulled back onto it. Empty: the default place.", synced: false);
        }

        public static Color Colour(ConfigEntry<string> entry, Color fallback)
        {
            return entry != null && ColorUtility.TryParseHtmlString(entry.Value, out Color colour) ? colour : fallback;
        }
    }
}
