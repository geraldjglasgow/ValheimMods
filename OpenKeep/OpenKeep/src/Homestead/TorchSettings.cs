using BepInEx.Configuration;
using SyncedConfig;

namespace OpenKeep.Homestead
{
    /// <summary>The torch keys of section "8. Homestead": they change fuel use and light for everybody, so they are synced and lockable.</summary>
    public static class TorchSettings
    {
        public const string Section = HomesteadModule.Section;

        public const string DefaultPieces = "piece_groundtorch_wood, piece_groundtorch, piece_groundtorch_green, piece_groundtorch_blue, piece_walltorch";

        public static ConfigEntry<bool> TorchesNightOnly { get; private set; }
        public static ConfigEntry<string> TorchPieces { get; private set; }

        public static void Bind(SyncedConfiguration synced)
        {
            TorchesNightOnly = synced.Bind(Section, "Torches Night Only", true,
                "The fires named in Torch Pieces are lit at nightfall and put out at daybreak (the game's own night). An unlit torch burns no fuel and gives no light, like an empty one. "
                + "Each torch is switched once per nightfall and daybreak, so a fire that can be switched by hand (the resin candle) keeps your choice until the next one; the game has no switch for the other torches. "
                + "Off: torches put out by this setting are lit again as soon as their area is loaded. Turn it off and visit your bases before removing OpenKeep, or torches put out by day stay dark.");
            TorchPieces = synced.Bind(Section, "Torch Pieces", DefaultPieces,
                "Prefab names, comma separated, of the fires Torches Night Only switches: the standing torches and the sconce by default. "
                + "Any fire may be added, for example piece_brazierfloor01, piece_brazierceiling01, piece_brazierfloor02, piece_jackoturnip, piece_snowlantern or Candle_resin; "
                + "campfires, hearths and braziers also give the warmth beds and resting need, which is gone while they are out.");
        }
    }
}
