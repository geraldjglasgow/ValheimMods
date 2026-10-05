using BepInEx.Configuration;
using SyncedConfig;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Section "14. Blueprints": the feature's switch and whether building costs materials, both synced, so the
    /// server's values bind every player while the configuration is locked. Off (the default): the hammer's build menu
    /// has no Blueprints tab and a command is refused. Everything else is fixed (<see cref="BlueprintRules"/>).
    /// </summary>
    public static class BlueprintSettings
    {
        public const string Section = "14. Blueprints";

        public static ConfigEntry<bool> EnabledEntry { get; private set; }
        public static ConfigEntry<bool> FreeMaterialsEntry { get; private set; }

        /// <summary>The server allows blueprints.</summary>
        public static bool Enabled => EnabledEntry != null && EnabledEntry.Value;

        /// <summary>Building from the Blueprints tab costs no materials and no stone for the ground.</summary>
        public static bool FreeMaterials => FreeMaterialsEntry != null && FreeMaterialsEntry.Value;

        public static void Bind(SyncedConfiguration synced)
        {
            EnabledEntry = synced.Bind(Section, "Enabled", false,
                "When on, the hammer's build menu gets a Blueprints tab: Fix ground, the Site planner, Copy building, and the blueprints in " +
                "BepInEx/config/OpenKeep.Blueprints with their folders (F2 renames). Placing one clears the site, shapes the ground to fit (cuts " +
                "hills, fills dips, digs water areas below sea level, within the game's 8 m), paints dirt under the building and places every " +
                "piece. Players pay the pieces' materials, must have learned the pieces and cannot build over wards or other buildings; " +
                "no-cost mode is free. Off: no Blueprints tab.");
            FreeMaterialsEntry = synced.Bind(Section, "Build Without Materials", false,
                "When on, blueprints and ground fixes from the Blueprints tab cost nothing: no building materials and no stone for raised ground " +
                "(and lowered ground gives no stone back). Pieces must still have been learned. Off: players pay every piece's materials, " +
                "and the ground work costs or gives Stone (raised ground costs it, lowered ground gives it back, the difference is paid).");
        }
    }

    /// <summary>The fixed numbers and keys of the blueprint feature (it has no settings of its own).</summary>
    public static class BlueprintRules
    {
        /// <summary>Cut slopes rise 1 m per metre (45 degrees) from the pad into higher ground.</summary>
        public const float CutSlope = 1f;

        /// <summary>Fill slopes fall 0.7 m per metre (about 35 degrees, loose earth) from the pad to lower ground.</summary>
        public const float FillSlope = 0.7f;

        /// <summary>The farthest the cut and fill slopes reach beyond the pad, metres.</summary>
        public const float SkirtReach = 24f;

        /// <summary>The game keeps the ground within this many metres of its generated height (TerrainComp's own clamp).</summary>
        public const float GameLimit = 8f;

        /// <summary>A piece whose bottom is this close above the pad (or lower) stands on the ground: dirt is painted under it.</summary>
        public const float GroundContact = 1f;

        /// <summary>Margin in metres around the pieces in which trees, rocks and shrubs are cleared.</summary>
        public const float ClearMargin = 1.5f;

        /// <summary>Ground with water stays at least this far above sea level (the shore of a canal site).</summary>
        public const float DryMargin = 0.5f;

        /// <summary>How far the crosshair finds ground for a blueprint, metres.</summary>
        public const float AimRange = 200f;

        /// <summary>Pieces placed per frame while building.</summary>
        public const int PiecesPerFrame = 80;

        /// <summary>Blueprints with more pieces preview only what stands near the ground.</summary>
        public const int FullPreviewLimit = 3000;

        /// <summary>The ground-level preview of a large blueprint shows pieces up to this height.</summary>
        public const float OutlineHeight = 1.2f;

        /// <summary>One press of the height keys moves the floor this far, metres (Shift: <see cref="HeightFastStep"/>).</summary>
        public const float HeightStep = 0.25f;

        public const float HeightFastStep = 2f;

        /// <summary>Stone per cubic metre of ground raised (paid) or lowered (given back); the difference is what counts.</summary>
        public const float StonePerCubicMetre = 0.5f;

        /// <summary>Alt + Left / Right turn a blueprint this many degrees, again every <see cref="FineRepeat"/> s while held.</summary>
        public const float FineTurnDegrees = 1f;
        public const float FineRepeatDelay = 0.35f;
        public const float FineRepeat = 0.05f;

        /// <summary>Fix ground: a piece whose bottom is at most this far above the ground there (or below it) touches the ground.</summary>
        public const float TouchHeight = 1.2f;

        /// <summary>Fix ground: a piece at most this tall is a floor; the ground under it is cut or filled to its bottom. Taller pieces only get ground filled up to them.</summary>
        public const float FloorHeight = 0.6f;

        /// <summary>Fix ground: the ground stays this far under a piece's bottom.</summary>
        public const float UnderGap = 0.05f;

        /// <summary>Fix ground: the building under the crosshair, at most this many pieces within this many metres.</summary>
        public const int FixMaxPieces = 4000;
        public const float FixReach = 90f;

        /// <summary>Fix ground: smoothing passes that round the slopes off (blueprints keep their slopes as planned).</summary>
        public const int FixSmoothPasses = 3;

        /// <summary>The default radius of 'openkeep blueprint save', metres.</summary>
        public const float SaveRadius = 15f;

        public const float SaveMaxRadius = 80f;

        // Keys while a blueprint is selected in the hammer's Blueprints tab: fixed, since the feature has one setting.
        public const KeyCode TurnRightKey = KeyCode.RightArrow;
        public const KeyCode TurnLeftKey = KeyCode.LeftArrow;
        public const KeyCode FaceKey = KeyCode.Home;
        public const KeyCode UpKey = KeyCode.PageUp;
        public const KeyCode DownKey = KeyCode.PageDown;
        public const KeyCode GroundKey = KeyCode.End;
        public const KeyCode ReleaseKey = KeyCode.Backspace;

        /// <summary>Renames the blueprint under the mouse in the build menu (the selected blueprint with the menu closed).</summary>
        public const KeyCode RenameKey = KeyCode.F2;
    }
}
