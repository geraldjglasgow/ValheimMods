using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// The torch keys of section "8. Homestead": the schedule changes fuel use and light for everybody, so it is synced
    /// and lockable; the switch key is each player's own.
    /// </summary>
    public static class TorchSettings
    {
        public const string Section = HomesteadModule.Section;

        public const string DefaultPieces = "piece_groundtorch_wood, piece_groundtorch, piece_groundtorch_green, piece_groundtorch_blue, piece_walltorch";

        /// <summary>The largest Torch Margin: torches still go out for a third of the day.</summary>
        public const float MaxMargin = 4f;

        public static ConfigEntry<bool> TorchesNightOnly { get; private set; }
        public static ConfigEntry<string> TorchPieces { get; private set; }
        public static ConfigEntry<float> TorchMargin { get; private set; }
        public static ConfigEntry<KeyboardShortcut> SwitchKey { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            TorchesNightOnly = synced.Bind(Section, "Torches Night Only", true,
                "The fires named in Torch Pieces are lit at nightfall and put out at daybreak (the game's own night, widened by Torch Margin). An unlit torch burns no fuel and gives no light, like an empty one. "
                + "A player can keep a torch lit day and night with Torch Switch Key while looking at it, and put it back on the schedule the same way. "
                + "Each torch is switched once per nightfall and daybreak, so a fire that can be switched by hand (the resin candle) keeps your choice until the next one. "
                + "Off: torches put out by this setting are lit again as soon as their area is loaded, and the torches kept lit stay marked for when it is on again. Turn it off and visit your bases before removing OpenKeep, or torches put out by day stay dark.");
            TorchPieces = synced.Bind(Section, "Torch Pieces", DefaultPieces,
                "Prefab names, comma separated, of the fires Torches Night Only switches: the standing torches and the sconce by default. "
                + "Any fire may be added, for example piece_brazierfloor01, piece_brazierceiling01, piece_brazierfloor02, piece_jackoturnip, piece_snowlantern or Candle_resin; "
                + "campfires, hearths and braziers also give the warmth beds and resting need, which is gone while they are out.");
            TorchMargin = synced.Bind(Section, "Torch Margin", 1f,
                "In-game hours by which Torches Night Only lights the torches before nightfall and puts them out after daybreak. An in-game hour is a 24th of the game's day: 75 seconds of its 30 minute day. "
                + "0 follows the game's own night exactly (dark from 85 % to 15 % of the day, 9 minutes).",
                acceptableValues: new AcceptableValueRange<float>(0f, MaxMargin));
            BindPlayer(synced);
        }

        private static void BindPlayer(SyncedConfiguration synced)
        {
            SwitchKey = synced.Bind(Section, "Torch Switch Key", new KeyboardShortcut(KeyCode.O),
                "Pressed while looking at a torch of Torch Pieces (outside the inventory): keeps it lit day and night, or puts a torch kept lit back on the night schedule (out at once by day). "
                + "Only while Torches Night Only is on, and not inside a ward you have no access to. Pick a key the game does not use (not the Use key).", false);
        }
    }
}
